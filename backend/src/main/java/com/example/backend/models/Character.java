package com.example.backend.models;

import com.example.backend.models.statusEffects.IStatusEffect;
import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.ArrayList;
import java.util.List;

@Data
@NoArgsConstructor
@AllArgsConstructor
@JsonIgnoreProperties(ignoreUnknown = true)
public class Character {
    private String id;
    private String name;
    private int currentHp;
    private int currentMana;
    private CharacterStats stats;
    private List<String> moves;
    private List<IStatusEffect> activeEffects = new ArrayList<>();

    public int getMaxHp() { return stats.getHealth() * 10; }
    public int getMaxMana() { return stats.getMagic() * 5; }
    public int getManaPerTurn() { return stats.getMagic(); }
}
