package com.example.backend.models;

import lombok.Data;

import java.util.List;

@Data
public class CharacterDefinition {
    private String id;
    private String name;
    private boolean startingClass;
    private CharacterStats baseStats;
    private CharacterStats statsPerLevel;
    private List<String> moves;
    private List<String> possibleDrops;
}
