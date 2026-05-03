package com.example.backend.dtos.responces;

import com.example.backend.models.runs.Run;
import com.example.backend.models.runs.RunStatus;

import java.util.List;
import java.util.UUID;

public record RunResponse(
        UUID runId,
        RunStatus status,
        String currentNodeId,
        List<EncounterNodeResponse> nodes,
        UUID activeCombatId
) {
    public static RunResponse from(Run run, UUID activeCombatId) {
        List<EncounterNodeResponse> nodes = run.getNodes().stream()
                .map(EncounterNodeResponse::from)
                .toList();
        return new RunResponse(run.getId(), run.getStatus(), run.getCurrentNodeId(), nodes, activeCombatId);
    }

    public static RunResponse from(Run run) {
        return from(run, null);
    }
}
