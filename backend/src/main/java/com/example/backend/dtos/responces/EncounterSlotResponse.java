package com.example.backend.dtos.responces;

import com.example.backend.models.encounters.EncounterSlot;

public record EncounterSlotResponse(
        int index,
        String enemyDefinitionId,
        String enemyName,
        int enemyLevel,
        int completions
) {
    public static EncounterSlotResponse from(int index, EncounterSlot slot) {
        return new EncounterSlotResponse(
                index,
                slot.getEnemyDefinitionId(),
                slot.getEnemyName(),
                slot.getEnemyLevel(),
                slot.getCompletions()
        );
    }
}
