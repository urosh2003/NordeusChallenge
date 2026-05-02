package com.example.backend.dtos.responces;

import com.example.backend.models.Equipment;

import java.util.List;

public record PlayerRunConfig(
        List<String> knownMoves,
        List<String> equippedMoves,
        List<String> inventory,
        Equipment equipment,
        String characterId
) {}
