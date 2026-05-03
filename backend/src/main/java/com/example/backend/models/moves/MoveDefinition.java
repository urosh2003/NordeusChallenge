package com.example.backend.models.moves;

import lombok.Data;

import java.util.ArrayList;
import java.util.List;

@Data
public class MoveDefinition {
    private String id;
    private String name;
    private String description;
    private Cost cost = new Cost();
    private List<StatusEffectDef> statusEffects = new ArrayList<>();
    private List<MainEffectDef> mainEffects = new ArrayList<>();

    @Data
    public static class Cost {
        private CostType costType = CostType.none;
        private int costValue = 0;
    }

    @Data
    public static class StatusEffectDef {
        private TargetType target;
        private EffectType type;
        private int value;
        private int duration;
    }

    @Data
    public static class MainEffectDef {
        private MainEffectType type;
        private TargetType target;
        private ResourceType resource;
        private int baseValue;
        private Scaling scaling;
        private StatType reducedBy;
    }

    @Data
    public static class Scaling {
        private StatType stat;
        private double multiplier = 1.0;
    }

    public enum CostType       { none, mana, health, stamina }
    public enum TargetType     { self, enemy }
    public enum StatType       { attack, defense, magic }
    public enum EffectType     { attack, defense, magic, bleed, poison }
    public enum MainEffectType { damage, heal, steal }
    public enum ResourceType   { health, mana }
}
