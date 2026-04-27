package com.example.backend.controllers;

import com.example.backend.dtos.responces.CombatResponse;
import com.example.backend.dtos.responces.RunConfigResponse;
import com.example.backend.dtos.responces.RunResponse;
import com.example.backend.services.RunService;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.UUID;

@RestController
@RequestMapping("${api.base-path:/api}/runs")
public class RunController {

    private final RunService runService;

    public RunController(RunService runService) {
        this.runService = runService;
    }

    /** Start a new run — generates the 5-encounter map. */
    @PostMapping
    public ResponseEntity<RunResponse> createRun() {
        return ResponseEntity.status(HttpStatus.CREATED).body(runService.createRun());
    }

    /** Get the current run state (map overview). */
    @GetMapping("/{runId}")
    public ResponseEntity<RunResponse> getRun(@PathVariable UUID runId) {
        return ResponseEntity.ok(runService.getRun(runId));
    }

    /**
     * Full static configuration for this run — call once when the run starts.
     * Returns all 5 enemy definitions (with computed stats and movesets), full
     * move definitions for every move that can appear, full item definitions for
     * every possible drop and every item the player currently owns, and the
     * player's current loadout (equipped moves, inventory, equipment).
     */
    @GetMapping("/{runId}/config")
    public ResponseEntity<RunConfigResponse> getRunConfig(@PathVariable UUID runId) {
        return ResponseEntity.ok(runService.getRunConfig(runId));
    }

    /**
     * Start (or replay) the combat for encounter at the given index.
     * Returns a CombatResponse — use the combatId for subsequent combat actions.
     */
    @PostMapping("/{runId}/encounters/{index}/start")
    public ResponseEntity<CombatResponse> startCombat(
            @PathVariable UUID runId,
            @PathVariable int index) {
        return ResponseEntity.status(HttpStatus.CREATED).body(runService.startCombat(runId, index));
    }

    /**
     * After winning the current encounter, advance to the next one on the map.
     * Call this when the player chooses "Continue" instead of "Replay".
     */
    @PostMapping("/{runId}/continue")
    public ResponseEntity<RunResponse> continueRun(@PathVariable UUID runId) {
        return ResponseEntity.ok(runService.continueRun(runId));
    }
}
