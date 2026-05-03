package com.example.backend.models.statusEffects;

import com.example.backend.models.combats.CombatEvent;
import com.example.backend.models.combats.CombatEventType;
import com.example.backend.models.characters.Character;
import lombok.Getter;
import lombok.Setter;

import java.util.List;

/**
 * Poison: deals 10% of max HP per turn (min 1). Duration decrements normally.
 * Stacks by adding durations together.
 */
@Getter
@Setter
public class PoisonStatus extends IStatusEffect {

    public PoisonStatus() {}

    public PoisonStatus(int duration) {
        super(duration);
    }

    @Override
    public void apply(Character character) {}

    @Override
    public void unapply(Character character) {}

    @Override
    public List<CombatEvent> onTick(Character character) {
        int damage = Math.max(1, character.getMaxHp() / 10);
        damage = Math.min(damage, character.getCurrentHp());
        character.setCurrentHp(character.getCurrentHp() - damage);
        return List.of(CombatEvent.of(CombatEventType.DAMAGE_DEALT)
                .with("targetId",    character.getId())
                .with("amount",      damage)
                .with("sourceMoveId", "poison"));
    }

    @Override public boolean isStackable() { return true; }

    @Override
    public void stack(int addValue, int addDuration) {
        duration += addDuration;
    }

    @Override public String getStatType() { return "poison"; }

    @Override public int getValue() { return 0; }
}
