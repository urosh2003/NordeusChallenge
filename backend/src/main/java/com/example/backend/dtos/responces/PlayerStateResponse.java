package com.example.backend.dtos.responces;

import com.example.backend.models.characters.CharacterStats;
import com.example.backend.models.items.Equipment;
import com.example.backend.models.players.PlayerState;

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
        int currentStamina,
        int maxHp,
        int maxMana,
        int maxStamina
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
                ps.getCurrentStamina(),
                0, 0, 0
        );
    }

    public static PlayerStateResponse from(PlayerState ps, int maxHp, int maxMana, int maxStamina) {
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
                ps.getCurrentStamina(),
                maxHp, maxMana, maxStamina
        );
    }
}
