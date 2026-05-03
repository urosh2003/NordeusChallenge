package com.example.backend.models.items;

import lombok.Data;

@Data
public class PassiveEffect {
    private PassiveEffectType type;
    private int value;
}
