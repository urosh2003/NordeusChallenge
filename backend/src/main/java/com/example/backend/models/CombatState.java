package com.example.backend.models;

import com.example.backend.combat.CombatEvent;
import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.ArrayList;
import java.util.List;

@Data
@NoArgsConstructor
@JsonIgnoreProperties(ignoreUnknown = true)
public class CombatState {
    private Character player;
    private Character enemy;
    // Last N combat events; used by the Drools AI for CEP and accumulate rules.
    private List<CombatEvent> eventHistory = new ArrayList<>();

    public CombatState(Character player, Character enemy) {
        this.player = player;
        this.enemy = enemy;
    }

    public void appendHistory(List<CombatEvent> events) {
        eventHistory.addAll(events);
        while (eventHistory.size() > 20) {
            eventHistory.remove(0);
        }
    }
}
