package com.example.backend.dtos.responces;

import com.example.backend.combat.MoveDefinition;
import com.example.backend.models.EnvironmentDefinition;
import com.example.backend.models.ItemDefinition;

import java.util.List;
import java.util.Map;
import java.util.UUID;

public record RunConfigResponse(
        UUID runId,
        List<EncounterConfig> encounters,
        Map<String, MoveDefinition> moves,
        Map<String, ItemDefinition> items,
        PlayerRunConfig player,
        Map<String, EnvironmentDefinition> environments
) {}
