package com.example.backend.services.ai;

public record ArchetypeParams(
        String name,
        double criticalHpPct,
        double retreatHpPct,
        double aggressionHpPct
) {}
