package com.example.backend.controllers;

import com.example.backend.dtos.requests.CreateRunRequest;
import com.example.backend.dtos.responces.*;
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

    @PostMapping
    public ResponseEntity<RunResponse> createRun(@RequestBody CreateRunRequest request) {
        return ResponseEntity.status(HttpStatus.CREATED).body(runService.createRun(request.classId()));
    }

    @GetMapping("/{runId}")
    public ResponseEntity<RunResponse> getRun(@PathVariable UUID runId) {
        return ResponseEntity.ok(runService.getRun(runId));
    }

    @GetMapping("/{runId}/config")
    public ResponseEntity<RunConfigResponse> getRunConfig(@PathVariable UUID runId) {
        return ResponseEntity.ok(runService.getRunConfig(runId));
    }

    /** Start combat for a COMBAT or BOSS node. */
    @PostMapping("/{runId}/nodes/{nodeId}/start")
    public ResponseEntity<CombatResponse> startCombat(
            @PathVariable UUID runId,
            @PathVariable String nodeId) {
        return ResponseEntity.status(HttpStatus.CREATED).body(runService.startCombat(runId, nodeId));
    }

    /** Enter a SHOP or REST_SITE node (auto-heals on REST, generates offers on SHOP). */
    @PostMapping("/{runId}/nodes/{nodeId}/enter")
    public ResponseEntity<NodeEnterResponse> enterNode(
            @PathVariable UUID runId,
            @PathVariable String nodeId) {
        return ResponseEntity.ok(runService.enterNode(runId, nodeId));
    }

    /** Buy an offer from the current shop node. */
    @PostMapping("/{runId}/shop/buy/{offerId}")
    public ResponseEntity<PlayerStateResponse> buyOffer(
            @PathVariable UUID runId,
            @PathVariable String offerId) {
        return ResponseEntity.ok(runService.buyOffer(runId, offerId));
    }

    /** Sell an inventory item at the current shop. */
    @PostMapping("/{runId}/shop/sell/{itemId}")
    public ResponseEntity<PlayerStateResponse> sellItem(
            @PathVariable UUID runId,
            @PathVariable String itemId) {
        return ResponseEntity.ok(runService.sellItem(runId, itemId));
    }
}
