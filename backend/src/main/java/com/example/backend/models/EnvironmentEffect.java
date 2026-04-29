package com.example.backend.models;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@JsonIgnoreProperties(ignoreUnknown = true)
public class EnvironmentEffect {

    public enum ResourceType { HEALTH, MANA, STAMINA }
    public enum EffectType   { GAIN, LOSE }

    private ResourceType resourceType;
    private EffectType   effectType;
    private int          amount;
    private boolean      isPercent = false;
}
