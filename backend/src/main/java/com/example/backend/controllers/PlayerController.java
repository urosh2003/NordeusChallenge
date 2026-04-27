package com.example.backend.controllers;

import com.example.backend.dtos.requests.EquipItemRequest;
import com.example.backend.dtos.requests.EquipMovesRequest;
import com.example.backend.dtos.requests.StatDistributionRequest;
import com.example.backend.dtos.responces.PlayerStateResponse;
import com.example.backend.services.PlayerService;
import com.example.backend.services.RunService;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("${api.base-path:/api}/player")
public class PlayerController {

    private final PlayerService playerService;
    private final RunService runService;

    public PlayerController(PlayerService playerService, RunService runService) {
        this.playerService = playerService;
        this.runService = runService;
    }

    @GetMapping
    public ResponseEntity<PlayerStateResponse> getPlayer() {
        return ResponseEntity.ok(PlayerStateResponse.from(playerService.getOrCreatePlayerState()));
    }

    /**
     * PUT /api/player/moves
     * Body: { "moveIds": ["slash", "shieldUp", "battleCry", "secondWind"] }
     * Forbidden while a combat is active.
     */
    @PutMapping("/moves")
    public ResponseEntity<?> equipMoves(@RequestBody EquipMovesRequest request) {
        if (runService.anyActiveCombat()) {
            return ResponseEntity.status(HttpStatus.CONFLICT)
                    .body("Cannot change equipped moves during an active combat");
        }
        return ResponseEntity.ok(PlayerStateResponse.from(playerService.equipMoves(request.moveIds())));
    }

    /**
     * POST /api/player/level-up
     * Body: { "health": 1, "attack": 1, "defense": 1, "magic": 0 }  (must sum to 3)
     */
    @PostMapping("/level-up")
    public ResponseEntity<PlayerStateResponse> distributeStatPoints(@RequestBody StatDistributionRequest request) {
        return ResponseEntity.ok(PlayerStateResponse.from(
                playerService.distributeStatPoints(request.health(), request.attack(),
                        request.defense(), request.magic())));
    }

    /**
     * POST /api/player/equip
     * Body: { "itemId": "iron_sword" }
     * Moves the item from inventory into its equipment slot.
     * If the slot was occupied the previous item goes back to inventory.
     */
    @PostMapping("/equip")
    public ResponseEntity<PlayerStateResponse> equipItem(@RequestBody EquipItemRequest request) {
        return ResponseEntity.ok(PlayerStateResponse.from(playerService.equipItem(request.itemId())));
    }

    /**
     * POST /api/player/unequip
     * Body: { "itemId": "iron_sword" }
     * Moves the item from its equipment slot back to inventory.
     */
    @PostMapping("/unequip")
    public ResponseEntity<PlayerStateResponse> unequipItem(@RequestBody EquipItemRequest request) {
        return ResponseEntity.ok(PlayerStateResponse.from(playerService.unequipItem(request.itemId())));
    }
}
