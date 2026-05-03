package com.example.backend.services.ai.facts;

import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
public class PlayerFact {
    private int currentHp;
    private int maxHp;
    private int attack;
    private int defense;
    private int magic;
}
