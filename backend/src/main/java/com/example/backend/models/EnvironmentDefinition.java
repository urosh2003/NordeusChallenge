package com.example.backend.models;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.ArrayList;
import java.util.List;

@Data
@NoArgsConstructor
@JsonIgnoreProperties(ignoreUnknown = true)
public class EnvironmentDefinition {
    private String id;
    private String name;
    private List<EnvironmentEffect> resourceEffects = new ArrayList<>();
    private CharacterStats          statEffects;     // null = no stat modifier
}
