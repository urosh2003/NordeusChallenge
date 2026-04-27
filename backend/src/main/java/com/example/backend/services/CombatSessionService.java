package com.example.backend.services;

import com.example.backend.ai.EnemyAIService;
import com.example.backend.combat.CombatEventType;
import com.example.backend.dtos.responces.CombatResponse;
import com.example.backend.dtos.responces.PlayerStateResponse;
import com.example.backend.combat.CombatEvent;
import com.example.backend.models.Character;
import com.example.backend.models.CombatInstance;
import com.example.backend.models.CombatState;
import com.example.backend.models.CombatStatus;
import com.example.backend.models.PlayerState;
import com.example.backend.models.Run;
import com.example.backend.models.Turn;
import com.example.backend.exceptions.CombatNotFoundException;
import com.example.backend.exceptions.InvalidMoveException;
import com.example.backend.exceptions.NotYourTurnException;
import com.example.backend.repositories.CombatInstanceRepository;
import com.example.backend.repositories.RunRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

@Service
@Transactional
public class CombatSessionService {

    private final CombatInstanceRepository repo;
    private final RunRepository runRepo;
    private final CombatService combatService;
    private final EnemyAIService enemyAI;
    private final CharacterLoader characterLoader;
    private final PlayerService playerService;

    public CombatSessionService(CombatInstanceRepository repo, RunRepository runRepo,
                                CombatService combatService, EnemyAIService enemyAI,
                                CharacterLoader characterLoader, PlayerService playerService) {
        this.repo = repo;
        this.runRepo = runRepo;
        this.combatService = combatService;
        this.enemyAI = enemyAI;
        this.characterLoader = characterLoader;
        this.playerService = playerService;
    }

    public CombatResponse createCombat(String enemyDefinitionId, int enemyLevel, UUID runId, int encounterIndex) {
        PlayerState playerState = playerService.getOrCreatePlayerState();
        Character player = playerService.buildCombatCharacter(playerState);
        Character enemy = characterLoader.createCharacter("enemy", enemyDefinitionId, enemyLevel);

        CombatState state = new CombatState(player, enemy);

        CombatInstance combat = new CombatInstance();
        combat.setStatus(CombatStatus.ACTIVE);
        combat.setCurrentTurn(Turn.PLAYER);
        combat.setState(state);
        combat.setEnemyLevel(enemyLevel);
        combat.setEnemyDefinitionId(enemyDefinitionId);
        combat.setRunId(runId);
        combat.setEncounterIndex(encounterIndex);

        combat = repo.save(combat);
        return new CombatResponse(combat.getId(), List.of(), state, PlayerStateResponse.from(playerState));
    }

    public CombatResponse processPlayerAction(UUID combatId, String moveId) {
        CombatInstance combat = repo.findById(combatId)
                .orElseThrow(() -> new CombatNotFoundException(combatId));

        if (combat.getStatus() != CombatStatus.ACTIVE) {
            throw new NotYourTurnException("Combat is already completed");
        }
        if (combat.getCurrentTurn() != Turn.PLAYER) {
            throw new NotYourTurnException("It is not the player's turn");
        }

        CombatState state = combat.getState();
        Character player = state.getPlayer();
        Character enemy = state.getEnemy();

        if (!player.getMoves().contains(moveId)) {
            throw new InvalidMoveException("Player does not know move: " + moveId);
        }

        List<CombatEvent> events = new ArrayList<>(combatService.executeMove(state, player, enemy, moveId));
        state.appendHistory(events);

        PlayerState playerState = playerService.getOrCreatePlayerState();
        PlayerStateResponse playerStateResponse = PlayerStateResponse.from(playerState);

        if (enemy.getCurrentHp() <= 0) {
            events.add(CombatEvent.of(CombatEventType.COMBAT_ENDED).with("winnerId", player.getId()));
            combat.setStatus(CombatStatus.COMPLETED);

            List<CombatEvent> rewardEvents = playerService.processVictory(
                    playerState, combat.getEnemyLevel(), enemy.getMoves(), combat.getEnemyDefinitionId());
            events.addAll(rewardEvents);
            playerStateResponse = PlayerStateResponse.from(playerState);

            if (combat.getRunId() != null) {
                runRepo.findById(combat.getRunId()).ifPresent(run -> {
                    run.getEncounters().get(combat.getEncounterIndex()).incrementCompletions();
                    runRepo.save(run);
                });
            }
        } else {
            combat.setCurrentTurn(Turn.ENEMY);
            events.add(CombatEvent.of(CombatEventType.TURN_CHANGED).with("newTurn", "ENEMY"));
            events.add(regenEvent(enemy));
            applyRegen(enemy);
        }

        combat.setState(state);
        repo.save(combat);
        return new CombatResponse(combat.getId(), events, state, playerStateResponse);
    }

    public CombatResponse processEnemyTurn(UUID combatId) {
        CombatInstance combat = repo.findById(combatId)
                .orElseThrow(() -> new CombatNotFoundException(combatId));

        if (combat.getStatus() != CombatStatus.ACTIVE) {
            throw new NotYourTurnException("Combat is already completed");
        }
        if (combat.getCurrentTurn() != Turn.ENEMY) {
            throw new NotYourTurnException("It is not the enemy's turn");
        }

        CombatState state = combat.getState();
        Character enemy = state.getEnemy();
        Character player = state.getPlayer();

        String pickedMove = enemyAI.pickMove(state, enemy, player, combat.getEnemyDefinitionId());
        List<CombatEvent> events = new ArrayList<>(combatService.executeMove(state, enemy, player, pickedMove));
        state.appendHistory(events);

        PlayerState playerState = playerService.getOrCreatePlayerState();

        if (player.getCurrentHp() <= 0) {
            events.add(CombatEvent.of(CombatEventType.COMBAT_ENDED).with("winnerId", enemy.getId()));
            combat.setStatus(CombatStatus.COMPLETED);
        } else {
            combat.setCurrentTurn(Turn.PLAYER);
            events.add(CombatEvent.of(CombatEventType.TURN_CHANGED).with("newTurn", "PLAYER"));
            events.add(regenEvent(player));
            applyRegen(player);
        }

        combat.setState(state);
        repo.save(combat);
        return new CombatResponse(combat.getId(), events, state, PlayerStateResponse.from(playerState));
    }

    // ── Turn-start resource regen ────────────────────────────────────────────

    private CombatEvent regenEvent(Character character) {
        int manaGain    = Math.min(character.getManaPerTurn(),
                character.getMaxMana()    - character.getCurrentMana());
        int staminaGain = Math.min(character.getStaminaPerTurn(),
                character.getMaxStamina() - character.getCurrentStamina());
        return CombatEvent.of(CombatEventType.RESOURCE_REGEN)
                .with("targetId",     character.getId())
                .with("manaGained",    manaGain)
                .with("staminaGained", staminaGain);
    }

    private void applyRegen(Character character) {
        character.setCurrentMana(Math.min(
                character.getMaxMana(), character.getCurrentMana() + character.getManaPerTurn()));
        character.setCurrentStamina(Math.min(
                character.getMaxStamina(), character.getCurrentStamina() + character.getStaminaPerTurn()));
    }

    public CombatResponse getCombat(UUID combatId) {
        CombatInstance combat = repo.findById(combatId)
                .orElseThrow(() -> new CombatNotFoundException(combatId));
        PlayerState playerState = playerService.getOrCreatePlayerState();
        return new CombatResponse(combat.getId(), List.of(), combat.getState(),
                PlayerStateResponse.from(playerState));
    }
}
