package com.example.backend.controllers;

import com.example.backend.dtos.requests.MoveRequest;
import com.example.backend.dtos.responces.CombatResponse;
import com.example.backend.services.combats.CombatSessionService;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.UUID;

@RestController
@RequestMapping("${api.base-path:/api}/combats")
public class CombatController {

    private final CombatSessionService combatSessionService;

    public CombatController(CombatSessionService combatSessionService) {
        this.combatSessionService = combatSessionService;
    }

    @PostMapping
    public ResponseEntity<CombatResponse> createCombat() {
        return ResponseEntity.status(HttpStatus.CREATED).body(
                combatSessionService.createCombat("witch", 1, null, null, null));
    }

    @PostMapping("/{combatId}/actions")
    public ResponseEntity<CombatResponse> playerAction(
            @PathVariable UUID combatId,
            @RequestBody MoveRequest request) {
        return ResponseEntity.ok(combatSessionService.processPlayerAction(combatId, request.moveId()));
    }

    @PostMapping("/{combatId}/pass")
    public ResponseEntity<CombatResponse> playerPass(@PathVariable UUID combatId) {
        return ResponseEntity.ok(combatSessionService.processPlayerPass(combatId));
    }

    @GetMapping("/{combatId}/enemy-turn")
    public ResponseEntity<CombatResponse> enemyTurn(@PathVariable UUID combatId) {
        return ResponseEntity.ok(combatSessionService.processEnemyTurn(combatId));
    }

    @GetMapping("/{combatId}")
    public ResponseEntity<CombatResponse> getCombat(@PathVariable UUID combatId) {
        return ResponseEntity.ok(combatSessionService.getCombat(combatId));
    }
}
