package com.example.backend.models;

import com.example.backend.models.statusEffects.IStatusEffect;
import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.ArrayList;
import java.util.List;

@Data
@NoArgsConstructor
@JsonIgnoreProperties(ignoreUnknown = true)
public class Character {
    private String id;
    private String name;
    private int currentHp;
    private int currentMana;
    private int currentStamina;
    private CharacterStats stats;
    private List<String> moves;
    private List<IStatusEffect> activeEffects = new ArrayList<>();

    // Item passive bonuses — applied during buildCombatCharacter, not serialised
    private int bonusMaxMana     = 0;
    private int bonusMaxStamina  = 0;
    private int bonusManaRegen   = 0;
    private int bonusStaminaRegen = 0;

    public Character(String id, String name, int currentHp, int currentMana, int currentStamina,
                     CharacterStats stats, List<String> moves, List<IStatusEffect> activeEffects) {
        this.id            = id;
        this.name          = name;
        this.currentHp     = currentHp;
        this.currentMana   = currentMana;
        this.currentStamina = currentStamina;
        this.stats         = stats;
        this.moves         = moves;
        this.activeEffects = activeEffects;
    }

    public int getMaxHp()          { return stats.getHealth()  * 10; }
    public int getMaxMana()        { return stats.getMagic()   * 5  + bonusMaxMana; }
    public int getManaPerTurn()    { return stats.getMagic()        + bonusManaRegen; }
    public int getMaxStamina()     { return stats.getAttack()  * 5  + bonusMaxStamina; }
    public int getStaminaPerTurn() { return stats.getAttack()       + bonusStaminaRegen; }
}
