package com.example.backend.dtos.responces;

import com.example.backend.models.EncounterNode;
import com.example.backend.models.EncounterType;
import com.example.backend.models.ShopOffer;

import java.util.List;

public record EncounterNodeResponse(
        String id,
        int row,
        int column,
        EncounterType type,
        String enemyDefinitionId,
        String enemyName,
        int enemyLevel,
        String environmentId,
        int completions,
        List<String> nextNodeIds,
        List<ShopOffer> shopOffers
) {
    public static EncounterNodeResponse from(EncounterNode node) {
        return new EncounterNodeResponse(
                node.getId(),
                node.getRow(),
                node.getColumn(),
                node.getType(),
                node.getEnemyDefinitionId(),
                node.getEnemyName(),
                node.getEnemyLevel(),
                node.getEnvironmentId(),
                node.getCompletions(),
                node.getNextNodeIds(),
                node.getShopOffers()
        );
    }
}
