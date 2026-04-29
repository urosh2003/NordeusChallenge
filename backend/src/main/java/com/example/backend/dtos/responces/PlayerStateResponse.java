package com.example.backend.dtos.responces;

import com.example.backend.models.CharacterStats;
import com.example.backend.models.Equipment;
import com.example.backend.models.PlayerState;

import java.util.List;
import java.util.UUID;

public record PlayerStateResponse(
        UUID id,
        String characterName,
        int level,
        int currentXp,
        int xpToNextLevel,
        CharacterStats stats,
        List<String> knownMoves,
        List<String> equippedMoves,
        int pendingStatPoints,
        List<String> inventory,
        Equipment equipment,
        int gold,
        int currentHp,
        int currentMana,
        int currentStamina
) {
    public static PlayerStateResponse from(PlayerState ps) {
        return new PlayerStateResponse(
                ps.getId(),
                ps.getCharacterName(),
                ps.getLevel(),
                ps.getCurrentXp(),
                ps.getXpToNextLevel(),
                ps.getStats(),
                ps.getKnownMoves(),
                ps.getEquippedMoves(),
                ps.getPendingStatPoints(),
                ps.getInventory(),
                ps.getEquipment(),
                ps.getGold(),
                ps.getCurrentHp(),
                ps.getCurrentMana(),
                ps.getCurrentStamina()
        );
    }
}
