package com.example.backend.combat.moves;

import com.example.backend.combat.*;
import com.example.backend.models.Character;
import com.example.backend.models.statusEffects.*;

import java.util.ArrayList;
import java.util.List;

public class DataDrivenMove extends IMove {

    private final MoveDefinition definition;

    public DataDrivenMove(MoveDefinition definition) {
        super(definition.getId());
        this.definition = definition;
    }

    @Override
    public boolean canExecute(MoveContext ctx) {
        MoveDefinition.Cost cost = definition.getCost();
        return switch (cost.getCostType()) {
            case mana    -> ctx.actor().getCurrentMana()    >= cost.getCostValue();
            case health  -> ctx.actor().getCurrentHp()      >  cost.getCostValue();
            case stamina -> ctx.actor().getCurrentStamina() >= cost.getCostValue();
            case none    -> true;
        };
    }

    @Override
    public List<CombatEvent> execute(MoveContext ctx) {
        List<CombatEvent> events = new ArrayList<>();
        Character actor = ctx.actor();
        Character target = ctx.target();

        events.add(CombatEvent.of(CombatEventType.MOVE_USED)
                .with("actorId", actor.getId())
                .with("moveId", definition.getId())
                .with("targetId", target.getId()));

        MoveDefinition.Cost cost = definition.getCost();
        applyCost(cost, actor);

        if (cost.getCostType() != MoveDefinition.CostType.none) {
            events.add(CombatEvent.of(CombatEventType.RESOURCE_SPENT)
                    .with("actorId", actor.getId())
                    .with("costType", cost.getCostType().name())
                    .with("amount",   cost.getCostValue()));
        }

        for (MoveDefinition.MainEffectDef effect : definition.getMainEffects()) {
            events.addAll(applyMainEffect(effect, actor, target));
        }

        for (MoveDefinition.StatusEffectDef def : definition.getStatusEffects()) {
            Character effectTarget = def.getTarget() == MoveDefinition.TargetType.self ? actor : target;

            // Try to stack into an existing effect of the same type
            IStatusEffect existing = effectTarget.getActiveEffects().stream()
                    .filter(e -> e.isStackable() && e.getStatType().equals(def.getType().name()))
                    .findFirst().orElse(null);

            IStatusEffect activeEffect;
            boolean stacked;
            if (existing != null) {
                existing.stack(def.getValue(), def.getDuration());
                activeEffect = existing;
                stacked = true;
            } else {
                activeEffect = buildStatusEffect(def);
                effectTarget.getActiveEffects().add(activeEffect);
                activeEffect.apply(effectTarget);
                stacked = false;
            }

            events.add(CombatEvent.of(CombatEventType.STATUS_EFFECT_APPLIED)
                    .with("targetId", effectTarget.getId())
                    .with("statType", activeEffect.getStatType())
                    .with("value",    activeEffect.getValue())
                    .with("duration", activeEffect.getDuration())
                    .with("stacked",  stacked));
        }

        return events;
    }

    private void applyCost(MoveDefinition.Cost cost, Character actor) {
        switch (cost.getCostType()) {
            case mana    -> actor.setCurrentMana(actor.getCurrentMana() - cost.getCostValue());
            case health  -> actor.setCurrentHp(actor.getCurrentHp() - cost.getCostValue());
            case stamina -> actor.setCurrentStamina(actor.getCurrentStamina() - cost.getCostValue());
            case none    -> { }
        }
    }

    private List<CombatEvent> applyMainEffect(MoveDefinition.MainEffectDef effect, Character actor, Character target) {
        List<CombatEvent> events = new ArrayList<>();
        int value = computeValue(effect, actor, target);

        switch (effect.getType()) {
            case damage -> {
                Character dmgTarget = effect.getTarget() == MoveDefinition.TargetType.self ? actor : target;
                dmgTarget.setCurrentHp(Math.max(0, dmgTarget.getCurrentHp() - value));
                events.add(CombatEvent.of(CombatEventType.DAMAGE_DEALT)
                        .with("targetId", dmgTarget.getId())
                        .with("amount", value)
                        .with("sourceMoveId", definition.getId()));
            }
            case heal -> {
                Character healTarget = effect.getTarget() == MoveDefinition.TargetType.self ? actor : target;
                int healed = Math.min(value, healTarget.getMaxHp() - healTarget.getCurrentHp());
                healTarget.setCurrentHp(healTarget.getCurrentHp() + healed);
                events.add(CombatEvent.of(CombatEventType.HEAL_RECEIVED)
                        .with("targetId", healTarget.getId())
                        .with("amount", healed)
                        .with("sourceMoveId", definition.getId()));
            }
            case steal -> {
                int stolen = Math.min(value, target.getCurrentHp());
                target.setCurrentHp(Math.max(0, target.getCurrentHp() - stolen));
                int healed = Math.min(stolen, actor.getMaxHp() - actor.getCurrentHp());
                actor.setCurrentHp(actor.getCurrentHp() + healed);
                events.add(CombatEvent.of(CombatEventType.DAMAGE_DEALT)
                        .with("targetId", target.getId())
                        .with("amount", stolen)
                        .with("sourceMoveId", definition.getId()));
                events.add(CombatEvent.of(CombatEventType.HEAL_RECEIVED)
                        .with("targetId", actor.getId())
                        .with("amount", healed)
                        .with("sourceMoveId", definition.getId()));
            }
        }

        return events;
    }

    private int computeValue(MoveDefinition.MainEffectDef effect, Character actor, Character target) {
        int value = effect.getBaseValue();

        if (effect.getScaling() != null) {
            int scalingStat = switch (effect.getScaling().getStat()) {
                case attack -> actor.getStats().getAttack();
                case magic -> actor.getStats().getMagic();
                case defense -> actor.getStats().getDefense();
            };
            value += (int) (scalingStat * effect.getScaling().getMultiplier());
        }

        if (effect.getReducedBy() != null) {
            int reduction = switch (effect.getReducedBy()) {
                case defense -> target.getStats().getDefense();
                case magic -> target.getStats().getMagic();
                case attack -> target.getStats().getAttack();
            };
            value = Math.max(0, value - reduction);
        }

        return value;
    }

    private IStatusEffect buildStatusEffect(MoveDefinition.StatusEffectDef def) {
        return switch (def.getType()) {
            case attack  -> new AttackStatus(def.getDuration(), def.getValue());
            case defense -> new DefenseStatus(def.getDuration(), def.getValue());
            case magic   -> new MagicStatus(def.getDuration(), def.getValue());
            case bleed   -> new BleedStatus(def.getValue());
            case poison  -> new PoisonStatus(def.getDuration());
        };
    }

}
