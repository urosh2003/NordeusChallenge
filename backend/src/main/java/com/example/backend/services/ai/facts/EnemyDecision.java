package com.example.backend.services.ai.facts;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class EnemyDecision {
    private String moveId;
    private int priority;
    private String reasoning;
}
