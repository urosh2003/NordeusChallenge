package com.example.backend.models.statusEffects;

import com.example.backend.models.Character;
import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class MagicStatus extends IStatusEffect {
    private int value;

    public MagicStatus() {}

    public MagicStatus(int duration, int value) {
        super(duration);
        this.value = value;
    }

    @Override
    public void apply(Character character) {
        character.getStats().updateMagic(value);
    }

    @Override
    public void unapply(Character character) {
        character.getStats().updateMagic(-value);
    }
}
