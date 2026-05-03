package com.example.backend.models.characters;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.Data;

import java.util.List;

@Data
public class CharacterDefinition {
    private String id;
    private String name;
    private boolean startingClass;
    @JsonProperty("isBoss")
    private boolean isBoss;
    private CharacterStats baseStats;
    private CharacterStats statsPerLevel;
    private List<String> moves;
    private List<String> possibleDrops;
}
