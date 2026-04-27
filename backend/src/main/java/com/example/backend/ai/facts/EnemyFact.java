package com.example.backend.ai.facts;

import com.example.backend.ai.Archetype;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
public class EnemyFact {
    private String id;
    private Archetype archetype;
    private double hpPercent;
    private int currentStamina;
    private int maxStamina;
    private int currentMana;
    private int maxMana;
    private int maxHp;
    private int attack;
    private int defense;
    private int magic;
}
