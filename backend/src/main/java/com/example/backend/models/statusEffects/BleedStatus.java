package com.example.backend.models.statusEffects;

import com.example.backend.combat.CombatEvent;
import com.example.backend.combat.CombatEventType;
import com.example.backend.models.Character;
import lombok.Getter;
import lombok.Setter;

import java.util.List;

/**
 * Bleed: deals `value` damage at end of turn, then decrements value by 1.
 * Expires when value reaches 0. Stacks by adding values together.
 * The `duration` field mirrors `value` so serialised CombatState shows the correct remaining amount.
 */
@Getter
@Setter
public class BleedStatus extends IStatusEffect {
    private int value;

    public BleedStatus() {}

    public BleedStatus(int bleedAmount) {
        super(bleedAmount);
        this.value = bleedAmount;
    }

    @Override
    public void apply(Character character) {}

    @Override
    public void unapply(Character character) {}

    @Override
    public List<CombatEvent> onTick(Character character) {
        int damage = Math.min(value, character.getCurrentHp());
        character.setCurrentHp(character.getCurrentHp() - damage);
        return List.of(CombatEvent.of(CombatEventType.DAMAGE_DEALT)
                .with("targetId",    character.getId())
                .with("amount",      damage)
                .with("sourceMoveId", "bleed"));
    }

    /** Decrements value (= bleed amount) instead of duration; keeps duration in sync. */
    @Override
    public boolean onEndTurn() {
        value--;
        duration = value;
        return value <= 0;
    }

    @Override public boolean isStackable() { return true; }

    @Override
    public void stack(int addValue, int addDuration) {
        value    += addValue;
        duration  = value;
    }

    @Override public String getStatType() { return "bleed"; }
}
