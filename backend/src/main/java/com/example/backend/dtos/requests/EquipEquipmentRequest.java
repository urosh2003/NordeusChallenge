package com.example.backend.dtos.requests;

public record EquipEquipmentRequest(
        String mainHand,
        String offHand,
        String armor,
        String gloves,
        String shoes,
        String amulet,
        String ring
) {}
