package com.example.backend.models.statusEffects;

import com.example.backend.models.Character;
import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class AttackStatus extends IStatusEffect {
    private int value;

    public AttackStatus() {}

    public AttackStatus(int duration, int value) {
        super(duration);
        this.value = value;
    }

    @Override
    public void apply(Character character) {
        character.getStats().updateAttack(value);
    }

    @Override
    public void unapply(Character character) {
        character.getStats().updateAttack(-value);
    }

    @Override public String getStatType() { return "attack"; }
}
