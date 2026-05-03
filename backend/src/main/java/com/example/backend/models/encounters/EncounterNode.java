package com.example.backend.models.encounters;

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
public class EncounterNode {
    private String id;
    private int row;
    private int column;
    private EncounterType type = EncounterType.COMBAT;
    private String enemyDefinitionId;
    private String enemyName;
    private int enemyLevel;
    private String environmentId; // COMBAT/BOSS only; null = no environment effects
    private int completions;
    private List<String> nextNodeIds = new ArrayList<>();
    private List<ShopOffer> shopOffers = new ArrayList<>();

    public void incrementCompletions() {
        completions++;
    }
}
