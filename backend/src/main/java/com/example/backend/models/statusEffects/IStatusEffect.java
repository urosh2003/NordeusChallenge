package com.example.backend.models.statusEffects;

import com.example.backend.combat.CombatEvent;
import com.example.backend.combat.CombatEventType;
import com.example.backend.models.Character;
import com.fasterxml.jackson.annotation.JsonSubTypes;
import com.fasterxml.jackson.annotation.JsonTypeInfo;
import lombok.Getter;
import lombok.Setter;

import java.util.Collections;
import java.util.List;

@JsonTypeInfo(use = JsonTypeInfo.Id.NAME, property = "effectType")
@JsonSubTypes({
        @JsonSubTypes.Type(value = AttackStatus.class,  name = "attack"),
        @JsonSubTypes.Type(value = DefenseStatus.class, name = "defense"),
        @JsonSubTypes.Type(value = MagicStatus.class,   name = "magic"),
        @JsonSubTypes.Type(value = BleedStatus.class,   name = "bleed"),
        @JsonSubTypes.Type(value = PoisonStatus.class,  name = "poison")
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
    public abstract String getStatType();
    public abstract int getValue();

    /** Called before onEndTurn — deal damage or apply per-turn effects. */
    public List<CombatEvent> onTick(Character character) { return Collections.emptyList(); }

    /** Decrement and return true when the effect should be removed. */
    public boolean onEndTurn() {
        duration--;
        return duration <= 0;
    }

    /** True if a second application merges into this instance instead of stacking separately. */
    public boolean isStackable() { return false; }

    /** Merge a new application into this existing effect. */
    public void stack(int addValue, int addDuration) {}
}
