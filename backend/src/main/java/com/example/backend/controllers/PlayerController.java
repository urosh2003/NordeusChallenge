package com.example.backend.controllers;

import com.example.backend.dtos.requests.EquipEquipmentRequest;
import com.example.backend.dtos.requests.EquipMovesRequest;
import com.example.backend.models.Equipment;
import com.example.backend.dtos.requests.StatDistributionRequest;
import com.example.backend.dtos.responces.PlayerStateResponse;
import com.example.backend.services.PlayerService;
import com.example.backend.services.RunService;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.UUID;

@RestController
@RequestMapping("${api.base-path:/api}/runs/{runId}/player")
public class PlayerController {

    private final PlayerService playerService;
    private final RunService runService;

    public PlayerController(PlayerService playerService, RunService runService) {
        this.playerService = playerService;
        this.runService = runService;
    }

    @GetMapping
    public ResponseEntity<PlayerStateResponse> getPlayer(@PathVariable UUID runId) {
        return ResponseEntity.ok(PlayerStateResponse.from(playerService.getOrCreatePlayerState(runId)));
    }

    @PutMapping("/moves")
    public ResponseEntity<?> equipMoves(@PathVariable UUID runId,
                                        @RequestBody EquipMovesRequest request) {
        if (runService.hasActiveCombat(runId)) {
            return ResponseEntity.status(HttpStatus.CONFLICT)
                    .body("Cannot change equipped moves during an active combat");
        }
        return ResponseEntity.ok(PlayerStateResponse.from(playerService.equipMoves(runId, request.moveIds())));
    }

    @PostMapping("/level-up")
    public ResponseEntity<PlayerStateResponse> distributeStatPoints(
            @PathVariable UUID runId,
            @RequestBody StatDistributionRequest request) {
        return ResponseEntity.ok(PlayerStateResponse.from(
                playerService.distributeStatPoints(runId, request.health(), request.attack(),
                        request.defense(), request.magic())));
    }

    @PutMapping("/equipment")
    public ResponseEntity<?> setEquipment(@PathVariable UUID runId,
                                          @RequestBody EquipEquipmentRequest request) {
        if (runService.hasActiveCombat(runId)) {
            return ResponseEntity.status(HttpStatus.CONFLICT)
                    .body("Cannot change equipment during an active combat");
        }
        Equipment incoming = new Equipment();
        incoming.setMainHand(request.mainHand());
        incoming.setOffHand(request.offHand());
        incoming.setArmor(request.armor());
        incoming.setGloves(request.gloves());
        incoming.setShoes(request.shoes());
        incoming.setAmulet(request.amulet());
        incoming.setRing(request.ring());
        return ResponseEntity.ok(PlayerStateResponse.from(playerService.setEquipment(runId, incoming)));
    }
}
