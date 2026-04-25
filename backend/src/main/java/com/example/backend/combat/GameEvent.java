package com.example.backend.combat;

import com.fasterxml.jackson.annotation.JsonAnyGetter;
import com.fasterxml.jackson.annotation.JsonAnySetter;

import java.util.LinkedHashMap;
import java.util.Map;

public class GameEvent {

    private GameEventType gameEventType;
    private final Map<String, Object> payload = new LinkedHashMap<>();

    public GameEvent() {}

    private GameEvent(GameEventType gameEventType) {
        this.gameEventType = gameEventType;
    }

    public static GameEvent of(GameEventType gameEventType) {
        return new GameEvent(gameEventType);
    }

    public GameEvent with(String key, Object value) {
        payload.put(key, value);
        return this;
    }

    @JsonAnyGetter
    public Map<String, Object> getPayload() { return payload; }

    @JsonAnySetter
    public void set(String key, Object value) { payload.put(key, value); }
}
