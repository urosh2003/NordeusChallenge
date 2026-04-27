package com.example.backend.ai;

public record ArchetypeParams(
        String name,
        double criticalHpPct,
        double retreatHpPct,
        double aggressionHpPct
) {}
