package com.example.backend.models.statusEffects;

import com.example.backend.models.Character;
import com.fasterxml.jackson.annotation.JsonSubTypes;
import com.fasterxml.jackson.annotation.JsonTypeInfo;
import lombok.Getter;
import lombok.Setter;

@JsonTypeInfo(use = JsonTypeInfo.Id.NAME, property = "effectType")
@JsonSubTypes({
        @JsonSubTypes.Type(value = AttackStatus.class, name = "attack"),
        @JsonSubTypes.Type(value = DefenseStatus.class, name = "defense"),
        @JsonSubTypes.Type(value = MagicStatus.class, name = "magic")
})
@Getter
@Setter
public abstract class IStatusEffect {
    protected int duration;

    protected IStatusEffect() {}

    public IStatusEffect(int duration) {
        this.duration = duration;
    }

    public abstract void apply(Character character);
    public abstract void unapply(Character character);

    public boolean onEndTurn() {
        duration--;
        return duration <= 0;
    }
}
