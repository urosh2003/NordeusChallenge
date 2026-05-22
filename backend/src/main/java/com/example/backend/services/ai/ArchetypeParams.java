package com.example.backend.services.ai;

import lombok.AllArgsConstructor;
import lombok.Data;

@Data
@AllArgsConstructor
public class ArchetypeParams {
    private String archetype;
    private double criticalHpPct;
    private double retreatHpPct;
    private double aggressionHpPct;
}
