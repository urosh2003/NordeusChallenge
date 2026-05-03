package com.example.backend.dtos.responces;

import com.example.backend.models.characters.CharacterStats;
import com.example.backend.models.encounters.EncounterType;

import java.util.List;

public record EncounterConfig(
        String nodeId,
        int row,
        int column,
        EncounterType type,
        String enemyDefinitionId,
        String enemyName,
        int enemyLevel,
        CharacterStats enemyStats,
        int enemyMaxHp,
        int enemyMaxMana,
        int enemyManaPerTurn,
        int enemyMaxStamina,
        int enemyStaminaPerTurn,
        List<String> enemyMoves,
        List<String> possibleDrops,
        int completions,
        List<String> nextNodeIds
) {}
