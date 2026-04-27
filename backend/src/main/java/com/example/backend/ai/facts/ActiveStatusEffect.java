package com.example.backend.ai.facts;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class ActiveStatusEffect {
    private String target;   // "player" or enemy id
    private String statType; // "attack", "defense", "magic"
    private int value;       // negative = debuff
}
