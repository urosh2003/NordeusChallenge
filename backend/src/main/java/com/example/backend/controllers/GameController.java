package com.example.backend.controllers;

import com.example.backend.dtos.requests.ActionRequest;
import com.example.backend.dtos.responces.GameResponse;
import com.example.backend.services.GameService;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.UUID;

@RestController
@RequestMapping("${api.base-path:/api}/games")
public class GameController {

    private final GameService gameService;

    public GameController(GameService gameService) {
        this.gameService = gameService;
    }

    @PostMapping
    public ResponseEntity<GameResponse> createGame() {
        return ResponseEntity.status(HttpStatus.CREATED).body(gameService.createGame());
    }

    @PostMapping("/{gameId}/actions")
    public ResponseEntity<GameResponse> playerAction(
            @PathVariable UUID gameId,
            @RequestBody ActionRequest request) {
        return ResponseEntity.ok(gameService.processPlayerAction(gameId, request.moveId()));
    }

    @GetMapping("/{gameId}/enemy-turn")
    public ResponseEntity<GameResponse> enemyTurn(@PathVariable UUID gameId) {
        return ResponseEntity.ok(gameService.processEnemyTurn(gameId));
    }

    @GetMapping("/{gameId}")
    public ResponseEntity<GameResponse> getGame(@PathVariable UUID gameId) {
        return ResponseEntity.ok(gameService.getGame(gameId));
    }
}
