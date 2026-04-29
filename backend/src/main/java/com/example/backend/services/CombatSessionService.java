package com.example.backend.services;

import com.example.backend.ai.EnemyAIService;
import com.example.backend.combat.CombatEventType;
import com.example.backend.dtos.responces.CombatResponse;
import com.example.backend.dtos.responces.PlayerStateResponse;
import com.example.backend.combat.CombatEvent;
import com.example.backend.models.Character;
import com.example.backend.models.CharacterStats;
import com.example.backend.models.CombatInstance;
import com.example.backend.models.CombatState;
import com.example.backend.models.CombatStatus;
import com.example.backend.models.EnvironmentDefinition;
import com.example.backend.models.EnvironmentEffect;
import com.example.backend.models.PlayerState;
import com.example.backend.models.Run;
import com.example.backend.models.RunStatus;
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
    private final EnvironmentLoader environmentLoader;

    public CombatSessionService(CombatInstanceRepository repo, RunRepository runRepo,
                                CombatService combatService, EnemyAIService enemyAI,
                                CharacterLoader characterLoader, PlayerService playerService,
                                EnvironmentLoader environmentLoader) {
        this.repo = repo;
        this.runRepo = runRepo;
        this.combatService = combatService;
        this.enemyAI = enemyAI;
        this.characterLoader = characterLoader;
        this.playerService = playerService;
        this.environmentLoader = environmentLoader;
    }

    public CombatResponse createCombat(String enemyDefinitionId, int enemyLevel, UUID runId,
                                       String encounterNodeId, String environmentId) {
        PlayerState playerState = playerService.getOrCreatePlayerState(runId);
        Character player = playerService.buildCombatCharacter(playerState);
        Character enemy = characterLoader.createCharacter("enemy", enemyDefinitionId, enemyLevel);

        if (environmentId != null) {
            EnvironmentDefinition env = environmentLoader.getDefinition(environmentId);
            applyStatEffects(player, env.getStatEffects());
            applyStatEffects(enemy,  env.getStatEffects());
        }

        CombatState state = new CombatState(player, enemy);

        CombatInstance combat = new CombatInstance();
        combat.setStatus(CombatStatus.ACTIVE);
        combat.setCurrentTurn(Turn.PLAYER);
        combat.setState(state);
        combat.setEnemyLevel(enemyLevel);
        combat.setEnemyDefinitionId(enemyDefinitionId);
        combat.setRunId(runId);
        combat.setEncounterNodeId(encounterNodeId);
        combat.setEnvironmentId(environmentId);

        combat = repo.save(combat);
        return new CombatResponse(combat.getId(), List.of(), state, PlayerStateResponse.from(playerState),
                Turn.PLAYER.name(), environmentId);
    }

    public CombatResponse processPlayerAction(UUID combatId, String moveId) {
        CombatInstance combat = repo.findById(combatId)
                .orElseThrow(() -> new CombatNotFoundException(combatId));

        if (combat.getStatus() != CombatStatus.ACTIVE)
            throw new NotYourTurnException("Combat is already completed");
        if (combat.getCurrentTurn() != Turn.PLAYER)
            throw new NotYourTurnException("It is not the player's turn");

        CombatState state = combat.getState();
        Character player = state.getPlayer();
        Character enemy = state.getEnemy();

        if (!player.getMoves().contains(moveId))
            throw new InvalidMoveException("Player does not know move: " + moveId);

        List<CombatEvent> events = new ArrayList<>(combatService.executeMove(state, player, enemy, moveId));
        state.appendHistory(events);

        PlayerState playerState = playerService.getOrCreatePlayerState(combat.getRunId());
        PlayerStateResponse playerStateResponse = PlayerStateResponse.from(playerState);

        if (enemy.getCurrentHp() <= 0) {
            events.add(CombatEvent.of(CombatEventType.COMBAT_ENDED).with("winnerId", player.getId()));
            combat.setStatus(CombatStatus.COMPLETED);

            List<CombatEvent> rewardEvents = playerService.processVictory(
                    playerState, combat.getEnemyLevel(), enemy.getMoves(), combat.getEnemyDefinitionId(), player);
            events.addAll(rewardEvents);
            playerStateResponse = PlayerStateResponse.from(playerState);

            if (combat.getRunId() != null) {
                runRepo.findById(combat.getRunId()).ifPresent(run -> {
                    run.getNodes().stream()
                            .filter(n -> n.getId().equals(combat.getEncounterNodeId()))
                            .findFirst()
                            .ifPresent(node -> {
                                node.incrementCompletions();
                                if (node.getRow() == 9) run.setStatus(RunStatus.COMPLETED);
                            });
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
        String currentTurn = combat.getStatus() == CombatStatus.ACTIVE ? combat.getCurrentTurn().name() : null;
        return new CombatResponse(combat.getId(), events, state, playerStateResponse, currentTurn, combat.getEnvironmentId());
    }

    public CombatResponse processEnemyTurn(UUID combatId) {
        CombatInstance combat = repo.findById(combatId)
                .orElseThrow(() -> new CombatNotFoundException(combatId));

        if (combat.getStatus() != CombatStatus.ACTIVE)
            throw new NotYourTurnException("Combat is already completed");
        if (combat.getCurrentTurn() != Turn.ENEMY)
            throw new NotYourTurnException("It is not the enemy's turn");

        CombatState state = combat.getState();
        Character enemy = state.getEnemy();
        Character player = state.getPlayer();

        String pickedMove = enemyAI.pickMove(state, enemy, player, combat.getEnemyDefinitionId());
        List<CombatEvent> events = new ArrayList<>(combatService.executeMove(state, enemy, player, pickedMove));
        state.appendHistory(events);

        PlayerState playerState = playerService.getOrCreatePlayerState(combat.getRunId());

        if (player.getCurrentHp() <= 0) {
            events.add(CombatEvent.of(CombatEventType.COMBAT_ENDED).with("winnerId", enemy.getId()));
            combat.setStatus(CombatStatus.COMPLETED);
        } else {
            // ── End of round: apply environment resource effects ─────────────
            if (combat.getEnvironmentId() != null) {
                EnvironmentDefinition env = environmentLoader.getDefinition(combat.getEnvironmentId());
                events.addAll(applyEnvironmentEffects(env, player, enemy));

                // Health drain can end the combat
                if (player.getCurrentHp() <= 0) {
                    events.add(CombatEvent.of(CombatEventType.COMBAT_ENDED).with("winnerId", enemy.getId()));
                    combat.setStatus(CombatStatus.COMPLETED);
                } else if (enemy.getCurrentHp() <= 0) {
                    events.add(CombatEvent.of(CombatEventType.COMBAT_ENDED).with("winnerId", player.getId()));
                    combat.setStatus(CombatStatus.COMPLETED);
                    List<CombatEvent> rewardEvents = playerService.processVictory(
                            playerState, combat.getEnemyLevel(), enemy.getMoves(),
                            combat.getEnemyDefinitionId(), player);
                    events.addAll(rewardEvents);
                    if (combat.getRunId() != null) {
                        runRepo.findById(combat.getRunId()).ifPresent(run -> {
                            run.getNodes().stream()
                                    .filter(n -> n.getId().equals(combat.getEncounterNodeId()))
                                    .findFirst()
                                    .ifPresent(node -> {
                                        node.incrementCompletions();
                                        if (node.getRow() == 9) run.setStatus(RunStatus.COMPLETED);
                                    });
                            runRepo.save(run);
                        });
                    }
                }
            }

            if (combat.getStatus() == CombatStatus.ACTIVE) {
                combat.setCurrentTurn(Turn.PLAYER);
                events.add(CombatEvent.of(CombatEventType.TURN_CHANGED).with("newTurn", "PLAYER"));
                events.add(regenEvent(player));
                applyRegen(player);
            }
        }

        combat.setState(state);
        repo.save(combat);
        String currentTurn = combat.getStatus() == CombatStatus.ACTIVE ? combat.getCurrentTurn().name() : null;
        return new CombatResponse(combat.getId(), events, state, PlayerStateResponse.from(playerState),
                currentTurn, combat.getEnvironmentId());
    }

    // ── Turn-start resource regen ────────────────────────────────────────────

    private CombatEvent regenEvent(Character character) {
        int manaGain    = Math.min(character.getManaPerTurn(),
                character.getMaxMana()    - character.getCurrentMana());
        int staminaGain = Math.min(character.getStaminaPerTurn(),
                character.getMaxStamina() - character.getCurrentStamina());
        return CombatEvent.of(CombatEventType.RESOURCE_REGEN)
                .with("targetId",      character.getId())
                .with("manaGained",    manaGain)
                .with("staminaGained", staminaGain);
    }

    private void applyRegen(Character character) {
        character.setCurrentMana(Math.min(
                character.getMaxMana(), character.getCurrentMana() + character.getManaPerTurn()));
        character.setCurrentStamina(Math.min(
                character.getMaxStamina(), character.getCurrentStamina() + character.getStaminaPerTurn()));
    }

    // ── Environment effects ──────────────────────────────────────────────────

    private void applyStatEffects(Character character, CharacterStats statEffects) {
        if (statEffects == null) return;
        CharacterStats s = character.getStats();
        s.updateHealth (statEffects.getHealth());
        s.updateAttack (statEffects.getAttack());
        s.updateDefense(statEffects.getDefense());
        s.updateMagic  (statEffects.getMagic());
        // Clamp current resources to the new maximums
        character.setCurrentHp     (Math.min(character.getCurrentHp(),      character.getMaxHp()));
        character.setCurrentMana   (Math.min(character.getCurrentMana(),     character.getMaxMana()));
        character.setCurrentStamina(Math.min(character.getCurrentStamina(),  character.getMaxStamina()));
    }

    private List<CombatEvent> applyEnvironmentEffects(EnvironmentDefinition env,
                                                       Character player, Character enemy) {
        List<CombatEvent> events = new ArrayList<>();
        for (EnvironmentEffect effect : env.getResourceEffects()) {
            events.add(applyResourceEffect(effect, player, env.getId()));
            events.add(applyResourceEffect(effect, enemy,  env.getId()));
        }
        return events;
    }

    private CombatEvent applyResourceEffect(EnvironmentEffect effect, Character character, String envId) {
        int delta = resolveAmount(effect, character);
        if (effect.getEffectType() == EnvironmentEffect.EffectType.LOSE) delta = -delta;

        switch (effect.getResourceType()) {
            case HEALTH  -> character.setCurrentHp(
                    Math.max(0, Math.min(character.getMaxHp(),      character.getCurrentHp()      + delta)));
            case MANA    -> character.setCurrentMana(
                    Math.max(0, Math.min(character.getMaxMana(),    character.getCurrentMana()    + delta)));
            case STAMINA -> character.setCurrentStamina(
                    Math.max(0, Math.min(character.getMaxStamina(), character.getCurrentStamina() + delta)));
        }

        return CombatEvent.of(CombatEventType.ENVIRONMENT_EFFECT)
                .with("environmentId",  envId)
                .with("targetId",       character.getId())
                .with("resourceType",   effect.getResourceType().name())
                .with("effectType",     effect.getEffectType().name())
                .with("amount",         delta);
    }

    private int resolveAmount(EnvironmentEffect effect, Character character) {
        if (!effect.isPercent()) return effect.getAmount();
        int max = switch (effect.getResourceType()) {
            case HEALTH  -> character.getMaxHp();
            case MANA    -> character.getMaxMana();
            case STAMINA -> character.getMaxStamina();
        };
        return Math.max(1, max * effect.getAmount() / 100);
    }

    public CombatResponse getCombat(UUID combatId) {
        CombatInstance combat = repo.findById(combatId)
                .orElseThrow(() -> new CombatNotFoundException(combatId));
        PlayerState playerState = playerService.getOrCreatePlayerState(combat.getRunId());
        String currentTurn = combat.getStatus() == CombatStatus.ACTIVE ? combat.getCurrentTurn().name() : null;
        return new CombatResponse(combat.getId(), List.of(), combat.getState(),
                PlayerStateResponse.from(playerState), currentTurn, combat.getEnvironmentId());
    }
}
