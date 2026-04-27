package com.example.backend.dtos.responces;

import com.example.backend.models.Run;
import com.example.backend.models.RunStatus;

import java.util.List;
import java.util.UUID;
import java.util.stream.IntStream;

public record RunResponse(
        UUID runId,
        RunStatus status,
        int currentEncounterIndex,
        List<EncounterSlotResponse> encounters
) {
    public static RunResponse from(Run run) {
        List<EncounterSlotResponse> slots = IntStream.range(0, run.getEncounters().size())
                .mapToObj(i -> EncounterSlotResponse.from(i, run.getEncounters().get(i)))
                .toList();
        return new RunResponse(run.getId(), run.getStatus(), run.getCurrentEncounterIndex(), slots);
    }
}
