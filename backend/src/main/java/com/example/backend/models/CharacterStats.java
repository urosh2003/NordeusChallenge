package com.example.backend.models;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
@JsonIgnoreProperties(ignoreUnknown = true)
public class CharacterStats {
    private int health;
    private int attack;
    private int defense;
    private int magic;

    public void updateHealth(int delta) { health += delta; }
    public void updateAttack(int delta) { attack += delta; }
    public void updateDefense(int delta) { defense += delta; }
    public void updateMagic(int delta) { magic += delta; }
}
