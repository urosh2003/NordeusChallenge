package com.example.backend.models.items;

import com.example.backend.models.characters.CharacterStats;
import lombok.Data;

import java.util.List;

@Data
public class ItemDefinition {
    private String id;
    private String name;
    private String description;
    private ItemType itemType;
    private CharacterStats bonusStats;
    private List<PassiveEffect> passiveEffects;
    private int cost;
    private boolean purchasable;
}
