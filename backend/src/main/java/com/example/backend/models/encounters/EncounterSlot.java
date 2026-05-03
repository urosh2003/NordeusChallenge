package com.example.backend.models.encounters;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
@JsonIgnoreProperties(ignoreUnknown = true)
public class EncounterSlot {
    private String enemyDefinitionId;
    private String enemyName;
    private int enemyLevel;
    private int completions;

    public void incrementCompletions() {
        completions++;
    }
}
