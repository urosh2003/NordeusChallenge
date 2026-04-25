package com.example.backend.services;

import com.example.backend.ai.EnemyAIService;
import com.example.backend.combat.GameEventType;
import com.example.backend.dtos.responces.GameResponse;
import com.example.backend.combat.GameEvent;
import com.example.backend.models.Character;
import com.example.backend.models.CombatState;
import com.example.backend.exceptions.GameNotFoundException;
import com.example.backend.exceptions.InvalidMoveException;
import com.example.backend.exceptions.NotYourTurnException;
import com.example.backend.models.GameInstance;
import com.example.backend.repositories.GameInstanceRepository;
import com.example.backend.models.GameStatus;
import com.example.backend.models.Turn;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;
import java.util.UUID;

@Service
@Transactional
public class GameService {

    private final GameInstanceRepository repo;
    private final CombatService combatService;
    private final EnemyAIService enemyAI;
    private final CharacterLoader characterLoader;

    public GameService(GameInstanceRepository repo, CombatService combatService, EnemyAIService enemyAI, CharacterLoader characterLoader) {
        this.repo = repo;
        this.combatService = combatService;
        this.enemyAI = enemyAI;
        this.characterLoader = characterLoader;
    }

    public GameResponse createGame() {
        Character player = characterLoader.createCharacter("player", "knight", 1);
        Character enemy = characterLoader.createCharacter("enemy", "witch", 1);

        CombatState state = new CombatState(player, enemy);

        GameInstance gameInstance = new GameInstance();
        gameInstance.setStatus(GameStatus.ACTIVE);
        gameInstance.setCurrentTurn(Turn.PLAYER);
        gameInstance.setState(state);

        gameInstance = repo.save(gameInstance);
        return new GameResponse(gameInstance.getId(), List.of(), state);
    }

    public GameResponse processPlayerAction(UUID gameId, String moveId) {
        GameInstance gameInstance = repo.findById(gameId)
                .orElseThrow(() -> new GameNotFoundException(gameId));

        if (gameInstance.getStatus() != GameStatus.ACTIVE) {
            throw new NotYourTurnException("Game is already completed");
        }
        if (gameInstance.getCurrentTurn() != Turn.PLAYER) {
            throw new NotYourTurnException("It is not the player's turn");
        }

        CombatState state = gameInstance.getState();
        Character player = state.getPlayer();
        Character enemy = state.getEnemy();

        if (!player.getMoves().contains(moveId)) {
            throw new InvalidMoveException("Player does not know move: " + moveId);
        }

        List<GameEvent> events = combatService.executeMove(state, player, enemy, moveId);

        if (enemy.getCurrentHp() <= 0) {
            events.add(GameEvent.of(GameEventType.COMBAT_ENDED).with("winnerId", player.getId()));
            gameInstance.setStatus(GameStatus.COMPLETED);
        } else {
            gameInstance.setCurrentTurn(Turn.ENEMY);
            events.add(GameEvent.of(GameEventType.TURN_CHANGED).with("newTurn", "ENEMY"));
        }

        gameInstance.setState(state);
        repo.save(gameInstance);
        return new GameResponse(gameInstance.getId(), events, state);
    }

    public GameResponse processEnemyTurn(UUID gameId) {
        GameInstance gameInstance = repo.findById(gameId)
                .orElseThrow(() -> new GameNotFoundException(gameId));

        if (gameInstance.getStatus() != GameStatus.ACTIVE) {
            throw new NotYourTurnException("Game is already completed");
        }
        if (gameInstance.getCurrentTurn() != Turn.ENEMY) {
            throw new NotYourTurnException("It is not the enemy's turn");
        }

        CombatState state = gameInstance.getState();
        Character enemy = state.getEnemy();
        Character player = state.getPlayer();

        String moveId = enemyAI.pickMove();
        List<GameEvent> events = combatService.executeMove(state, enemy, player, moveId);

        if (player.getCurrentHp() <= 0) {
            events.add(GameEvent.of(GameEventType.COMBAT_ENDED).with("winnerId", enemy.getId()));
            gameInstance.setStatus(GameStatus.COMPLETED);
        } else {
            gameInstance.setCurrentTurn(Turn.PLAYER);
            events.add(GameEvent.of(GameEventType.TURN_CHANGED).with("newTurn", "PLAYER"));
        }

        gameInstance.setState(state);
        repo.save(gameInstance);
        return new GameResponse(gameInstance.getId(), events, state);
    }

    @Transactional(readOnly = true)
    public GameResponse getGame(UUID gameId) {
        GameInstance gameInstance = repo.findById(gameId)
                .orElseThrow(() -> new GameNotFoundException(gameId));
        return new GameResponse(gameInstance.getId(), List.of(), gameInstance.getState());
    }
}
