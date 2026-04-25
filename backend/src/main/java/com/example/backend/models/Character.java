package com.example.backend.models;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.List;

@Data
@NoArgsConstructor
@AllArgsConstructor
@JsonIgnoreProperties(ignoreUnknown = true)
public class Character {
    private String id;
    private String name;
    private int currentHp;
    private int maxHp;
    private int currentMana;
    private int maxMana;
    private CharacterStats stats;
    private List<String> moves;
}
