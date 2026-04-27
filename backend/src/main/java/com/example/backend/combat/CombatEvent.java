package com.example.backend.combat;

import com.fasterxml.jackson.annotation.JsonAnyGetter;
import com.fasterxml.jackson.annotation.JsonAnySetter;
import com.fasterxml.jackson.annotation.JsonProperty;

import java.util.LinkedHashMap;
import java.util.Map;

public class CombatEvent {

    private CombatEventType combatEventType;
    private final Map<String, Object> payload = new LinkedHashMap<>();

    public CombatEvent() {}

    private CombatEvent(CombatEventType combatEventType) {
        this.combatEventType = combatEventType;
    }

    public static CombatEvent of(CombatEventType combatEventType) {
        return new CombatEvent(combatEventType);
    }

    public CombatEvent with(String key, Object value) {
        payload.put(key, value);
        return this;
    }

    @JsonProperty("type")
    public CombatEventType getType() { return combatEventType; }

    @JsonProperty("type")
    public void setType(CombatEventType type) { this.combatEventType = type; }

    @JsonAnyGetter
    public Map<String, Object> getPayload() { return payload; }

    @JsonAnySetter
    public void set(String key, Object value) { payload.put(key, value); }
}
