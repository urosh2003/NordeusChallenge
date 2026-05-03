package com.example.backend.services.ai;

import com.example.backend.services.ai.facts.*;
import com.example.backend.models.combats.CombatEvent;
import com.example.backend.models.moves.MoveDefinition;
import com.example.backend.services.moves.MoveRegistry;
import com.example.backend.models.characters.Character;
import com.example.backend.models.combats.CombatState;
import com.example.backend.models.statusEffects.AttackStatus;
import com.example.backend.models.statusEffects.DefenseStatus;
import com.example.backend.models.statusEffects.IStatusEffect;
import com.example.backend.models.statusEffects.MagicStatus;
import org.kie.api.KieBase;
import org.kie.api.KieServices;
import org.kie.api.runtime.KieSession;
import org.kie.api.runtime.KieSessionConfiguration;
import org.kie.api.runtime.conf.ClockTypeOption;
import org.kie.api.runtime.rule.EntryPoint;
import org.kie.api.time.SessionPseudoClock;
import org.springframework.stereotype.Service;

import java.util.Collection;
import java.util.Comparator;
import java.util.List;
import java.util.Map;
import java.util.concurrent.TimeUnit;

@Service
public class DroolsEnemyAIService {

    private static final Map<String, Archetype> ARCHETYPE_MAP = Map.of(
            "goblinWarrior",  Archetype.PHYSICAL_BRAWLER,
            "goblinMage",     Archetype.MAGIC_CASTER,
            "witch",          Archetype.MAGIC_DRAINER,
            "giantSpider",    Archetype.PHYSICAL_SKIRMISHER,
            "dragon",         Archetype.BALANCED_TANK,
            "archdemon",         Archetype.MAGIC_DRAINER,
            "skeletonWarrior",         Archetype.PHYSICAL_BRAWLER,
            "minotaur",         Archetype.PHYSICAL_BRAWLER,
            "centaur",         Archetype.PHYSICAL_BRAWLER,
            "skeletonArcher",         Archetype.BALANCED_TANK
    );

    private final KieBase kieBase;
    private final MoveRegistry moveRegistry;

    public DroolsEnemyAIService(KieBase kieBase, MoveRegistry moveRegistry) {
        this.kieBase = kieBase;
        this.moveRegistry = moveRegistry;
    }

    public String pickMove(CombatState state, Character enemy, Character player,
                           String enemyDefinitionId) {
        KieSessionConfiguration cfg = KieServices.Factory.get().newKieSessionConfiguration();
        cfg.setOption(ClockTypeOption.get("pseudo"));
        KieSession session = kieBase.newKieSession(cfg, null);
        try {
            insertFacts(session, state, enemy, player, enemyDefinitionId);
            session.fireAllRules();
            return bestDecision(session, enemy, player);
        } finally {
            session.dispose();
        }
    }

    // ── fact construction ────────────────────────────────────────────────────

    private void insertFacts(KieSession session, CombatState state,
                             Character enemy, Character player,
                             String enemyDefinitionId) {
        // EnemyFact
        EnemyFact ef = new EnemyFact();
        ef.setId(enemy.getId());
        ef.setArchetype(ARCHETYPE_MAP.getOrDefault(enemyDefinitionId, Archetype.BALANCED_TANK));
        ef.setMaxHp(enemy.getMaxHp());
        ef.setHpPercent(enemy.getMaxHp() > 0
                ? (double) enemy.getCurrentHp() / enemy.getMaxHp() : 0);
        ef.setCurrentStamina(enemy.getCurrentStamina());
        ef.setMaxStamina(enemy.getMaxStamina());
        ef.setCurrentMana(enemy.getCurrentMana());
        ef.setMaxMana(enemy.getMaxMana());
        ef.setAttack(enemy.getStats().getAttack());
        ef.setDefense(enemy.getStats().getDefense());
        ef.setMagic(enemy.getStats().getMagic());
        session.insert(ef);

        // PlayerFact
        PlayerFact pf = new PlayerFact();
        pf.setCurrentHp(player.getCurrentHp());
        pf.setMaxHp(player.getMaxHp());
        pf.setAttack(player.getStats().getAttack());
        pf.setDefense(player.getStats().getDefense());
        pf.setMagic(player.getStats().getMagic());
        session.insert(pf);

        // MoveOptions — one per usable move
        for (String moveId : enemy.getMoves()) {
            MoveDefinition def = safeGetDefinition(moveId);
            if (def == null) continue;
            if (!canAfford(def, enemy)) continue;

            MoveOption mo = new MoveOption();
            mo.setMoveId(moveId);
            mo.setMoveCategory(categorize(def));
            mo.setCostType(def.getCost().getCostType().name());
            mo.setCostValue(def.getCost().getCostValue());
            mo.setProjectedValue(projectedValue(def, enemy, player));
            session.insert(mo);
        }

        // ActiveStatusEffect facts (for backward chaining)
        insertStatusEffects(session, player.getId(), player.getActiveEffects());
        insertStatusEffects(session, enemy.getId(), enemy.getActiveEffects());

        // Combat history → event stream
        List<CombatEvent> history = state.getEventHistory();
        EntryPoint stream = session.getEntryPoint("combat-stream");
        SessionPseudoClock clock = session.getSessionClock();

        String currentSource = null;
        for (int i = 0; i < history.size(); i++) {
            CombatEvent ev = history.get(i);
            if (ev.getType() == null) continue;
            clock.advanceTime(1, TimeUnit.MILLISECONDS);

            CombatEventFact fact = new CombatEventFact();
            fact.setType(ev.getType().name());
            fact.setTurnTimestamp(i);

            Map<String, Object> payload = ev.getPayload();
            switch (ev.getType()) {
                case MOVE_USED -> {
                    currentSource = (String) payload.get("actorId");
                    fact.setSource(currentSource);
                    fact.setTarget((String) payload.get("targetId"));
                    String moveId = (String) payload.get("moveId");
                    if (moveId != null) {
                        MoveDefinition def = safeGetDefinition(moveId);
                        if (def != null) fact.setMoveCategory(categorize(def));
                    }
                }
                case DAMAGE_DEALT -> {
                    fact.setSource(currentSource);
                    fact.setTarget((String) payload.get("targetId"));
                    Object amt = payload.get("amount");
                    if (amt instanceof Number n) fact.setAmount(n.intValue());
                }
                default -> {
                    fact.setSource(currentSource);
                    fact.setTarget((String) payload.get("targetId"));
                }
            }
            stream.insert(fact);
        }
    }

    private void insertStatusEffects(KieSession session, String targetId,
                                     List<IStatusEffect> effects) {
        for (IStatusEffect eff : effects) {
            String statType;
            int value;
            if (eff instanceof AttackStatus a)  { statType = "attack";  value = a.getValue(); }
            else if (eff instanceof DefenseStatus d) { statType = "defense"; value = d.getValue(); }
            else if (eff instanceof MagicStatus m)   { statType = "magic";   value = m.getValue(); }
            else continue;
            session.insert(new ActiveStatusEffect(targetId, statType, value));
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private boolean canAfford(MoveDefinition def, Character actor) {
        return switch (def.getCost().getCostType()) {
            case mana    -> actor.getCurrentMana()    >= def.getCost().getCostValue();
            case stamina -> actor.getCurrentStamina() >= def.getCost().getCostValue();
            case health  -> actor.getCurrentHp()      >  def.getCost().getCostValue();
            case none    -> true;
        };
    }

    private MoveCategory categorize(MoveDefinition def) {
        for (MoveDefinition.MainEffectDef e : def.getMainEffects()) {
            if (e.getType() == MoveDefinition.MainEffectType.steal) return MoveCategory.DRAIN;
            if (e.getType() == MoveDefinition.MainEffectType.heal)  return MoveCategory.HEAL;
            if (e.getType() == MoveDefinition.MainEffectType.damage) {
                boolean magic = e.getScaling() != null
                        && e.getScaling().getStat() == MoveDefinition.StatType.magic;
                return magic ? MoveCategory.DAMAGE_MAGIC : MoveCategory.DAMAGE_PHYSICAL;
            }
        }
        boolean enemyDebuff = def.getStatusEffects().stream()
                .anyMatch(s -> s.getTarget() == MoveDefinition.TargetType.enemy && s.getValue() < 0);
        return enemyDebuff ? MoveCategory.ENEMY_DEBUFF : MoveCategory.SELF_BUFF;
    }

    private int projectedValue(MoveDefinition def, Character actor, Character target) {
        int total = 0;
        for (MoveDefinition.MainEffectDef e : def.getMainEffects()) {
            int v = e.getBaseValue();
            if (e.getScaling() != null) {
                int scalingStat = switch (e.getScaling().getStat()) {
                    case attack  -> actor.getStats().getAttack();
                    case magic   -> actor.getStats().getMagic();
                    case defense -> actor.getStats().getDefense();
                };
                v += (int) (scalingStat * e.getScaling().getMultiplier());
            }
            if (e.getReducedBy() != null) {
                int reduction = switch (e.getReducedBy()) {
                    case defense -> 0;
                    case magic   -> 0;
                    case attack  -> target.getStats().getDefense();
                };
                v = Math.max(0, v - reduction);
            }
            total += v;
        }
        return total;
    }

    private MoveDefinition safeGetDefinition(String moveId) {
        try { return moveRegistry.getDefinition(moveId); }
        catch (Exception e) { return null; }
    }

    private String bestDecision(KieSession session, Character enemy, Character player) {
        @SuppressWarnings("unchecked")
        Collection<EnemyDecision> decisions =
                (Collection<EnemyDecision>) session.getObjects(
                        obj -> obj instanceof EnemyDecision);

        if (!decisions.isEmpty()) {
            return decisions.stream()
                    .max(Comparator.comparingInt(EnemyDecision::getPriority))
                    .map(EnemyDecision::getMoveId)
                    .orElse(null);
        }
        // Hard fallback: first usable move
        return enemy.getMoves().stream()
                .filter(id -> {
                    MoveDefinition def = safeGetDefinition(id);
                    return def != null && canAfford(def, enemy);
                })
                .findFirst()
                .orElse(enemy.getMoves().get(0));
    }
}
