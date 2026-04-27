package com.example.backend.services;

import com.example.backend.combat.MoveDefinition;
import com.example.backend.combat.MoveRegistry;
import com.example.backend.dtos.responces.*;
import com.example.backend.models.*;
import com.example.backend.repositories.CombatInstanceRepository;
import com.example.backend.repositories.RunRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import com.example.backend.models.Character;

import java.util.*;
import java.util.stream.Collectors;

@Service
@Transactional
public class RunService {

    private static final List<String[]> ENEMY_POOL = List.of(
            new String[]{"witch",        "Witch"},
            new String[]{"giantSpider",  "Giant Spider"},
            new String[]{"dragon",       "Dragon"},
            new String[]{"goblinWarrior","Goblin Warrior"},
            new String[]{"goblinMage",   "Goblin Mage"}
    );

    private final RunRepository runRepo;
    private final CombatInstanceRepository combatRepo;
    private final CombatSessionService combatSessionService;
    private final CharacterLoader characterLoader;
    private final MoveRegistry moveRegistry;
    private final ItemRegistry itemRegistry;
    private final PlayerService playerService;

    public RunService(RunRepository runRepo, CombatInstanceRepository combatRepo,
                      CombatSessionService combatSessionService, CharacterLoader characterLoader,
                      MoveRegistry moveRegistry, ItemRegistry itemRegistry,
                      PlayerService playerService) {
        this.runRepo = runRepo;
        this.combatRepo = combatRepo;
        this.combatSessionService = combatSessionService;
        this.characterLoader = characterLoader;
        this.moveRegistry = moveRegistry;
        this.itemRegistry = itemRegistry;
        this.playerService = playerService;
    }

    /** Creates a new run: shuffles the 5 enemy types and assigns them levels 1–5 in order. */
    public RunResponse createRun() {
        List<String[]> shuffled = new ArrayList<>(ENEMY_POOL);
        Collections.shuffle(shuffled);

        List<EncounterSlot> encounters = new ArrayList<>();
        for (int i = 0; i < shuffled.size(); i++) {
            String[] enemy = shuffled.get(i);
            encounters.add(new EncounterSlot(enemy[0], enemy[1], i + 1, 0));
        }

        Run run = new Run();
        run.setEncounters(encounters);
        run = runRepo.save(run);
        return RunResponse.from(run);
    }

    @Transactional(readOnly = true)
    public RunResponse getRun(UUID runId) {
        return RunResponse.from(loadRun(runId));
    }

    /**
     * Start (or replay) a combat for the given encounter index.
     * Rules:
     *  - No active combat may already exist for this run.
     *  - index must be <= currentEncounterIndex (can't skip ahead).
     *  - First-time fight: index == currentEncounterIndex.
     *  - Replay: completions > 0 on any already-cleared index.
     */
    public CombatResponse startCombat(UUID runId, int index) {
        Run run = loadRun(runId);

        if (run.getStatus() == RunStatus.COMPLETED) {
            throw new IllegalStateException("Run is already completed");
        }
        if (combatRepo.existsByRunIdAndStatus(runId, CombatStatus.ACTIVE)) {
            throw new IllegalStateException("A combat is already in progress for this run");
        }
        if (index < 0 || index >= run.getEncounters().size()) {
            throw new IllegalArgumentException("Invalid encounter index: " + index);
        }

        EncounterSlot slot = run.getEncounters().get(index);
        boolean isCurrentEncounter = (index == run.getCurrentEncounterIndex());
        boolean isReplay = (slot.getCompletions() > 0);

        if (!isCurrentEncounter && !isReplay) {
            throw new IllegalStateException("Encounter " + index + " is not yet available");
        }

        return combatSessionService.createCombat(
                slot.getEnemyDefinitionId(), slot.getEnemyLevel(), runId, index);
    }

    /**
     * Advance to the next encounter after winning the current one.
     * Call when the player chooses "Continue" instead of "Replay".
     */
    public RunResponse continueRun(UUID runId) {
        Run run = loadRun(runId);

        if (run.getStatus() == RunStatus.COMPLETED) {
            throw new IllegalStateException("Run is already completed");
        }
        if (combatRepo.existsByRunIdAndStatus(runId, CombatStatus.ACTIVE)) {
            throw new IllegalStateException("Cannot continue while a combat is in progress");
        }

        EncounterSlot current = run.getEncounters().get(run.getCurrentEncounterIndex());
        if (current.getCompletions() == 0) {
            throw new IllegalStateException("Current encounter has not been completed yet");
        }

        int nextIndex = run.getCurrentEncounterIndex() + 1;
        if (nextIndex >= run.getEncounters().size()) {
            run.setStatus(RunStatus.COMPLETED);
        } else {
            run.setCurrentEncounterIndex(nextIndex);
        }

        return RunResponse.from(runRepo.save(run));
    }

    /** Whether any combat at all is currently active (used to block move-equipping). */
    @Transactional(readOnly = true)
    public boolean anyActiveCombat() {
        return combatRepo.existsByStatus(CombatStatus.ACTIVE);
    }

    /**
     * Returns everything the client needs to set up the full run UI in one call:
     * all 5 enemy definitions with computed stats, full move definitions for every
     * move that appears (enemy or player), full item definitions for every possible
     * drop and every item the player already owns, and the player's current loadout.
     */
    @Transactional(readOnly = true)
    public RunConfigResponse getRunConfig(UUID runId) {
        Run run = loadRun(runId);
        PlayerState playerState = playerService.getOrCreatePlayerState();

        Set<String> allMoveIds = new LinkedHashSet<>();
        Set<String> allItemIds = new LinkedHashSet<>();

        // Build one EncounterConfig per slot
        List<EncounterConfig> encounterConfigs = new ArrayList<>();
        List<EncounterSlot> slots = run.getEncounters();
        for (int i = 0; i < slots.size(); i++) {
            EncounterSlot slot = slots.get(i);
            CharacterDefinition def = characterLoader.getDefinition(slot.getEnemyDefinitionId());

            // Reuse CharacterLoader's stat computation via createCharacter
            Character enemy = characterLoader.createCharacter("_", slot.getEnemyDefinitionId(), slot.getEnemyLevel());
            CharacterStats stats = enemy.getStats();

            List<String> drops = def.getPossibleDrops() != null ? def.getPossibleDrops() : List.of();

            encounterConfigs.add(new EncounterConfig(
                    i,
                    slot.getEnemyDefinitionId(),
                    slot.getEnemyName(),
                    slot.getEnemyLevel(),
                    stats,
                    enemy.getMaxHp(),
                    enemy.getMaxMana(),
                    enemy.getManaPerTurn(),
                    enemy.getMaxStamina(),
                    enemy.getStaminaPerTurn(),
                    def.getMoves(),
                    drops,
                    slot.getCompletions()
            ));

            allMoveIds.addAll(def.getMoves());
            allItemIds.addAll(drops);
        }

        // Include everything the player already knows/owns
        allMoveIds.addAll(playerState.getKnownMoves());
        allItemIds.addAll(playerState.getInventory());
        allItemIds.addAll(playerState.getEquipment().getAllEquipped());

        Map<String, MoveDefinition> moves = allMoveIds.stream()
                .collect(Collectors.toMap(id -> id, moveRegistry::getDefinition,
                        (a, b) -> a, LinkedHashMap::new));

        Map<String, ItemDefinition> items = allItemIds.stream()
                .filter(itemRegistry::exists)
                .collect(Collectors.toMap(id -> id, itemRegistry::getItem,
                        (a, b) -> a, LinkedHashMap::new));

        PlayerRunConfig player = new PlayerRunConfig(
                playerState.getKnownMoves(),
                playerState.getEquippedMoves(),
                playerState.getInventory(),
                playerState.getEquipment()
        );

        return new RunConfigResponse(run.getId(), encounterConfigs, moves, items, player);
    }

    private Run loadRun(UUID runId) {
        return runRepo.findById(runId)
                .orElseThrow(() -> new IllegalArgumentException("Run not found: " + runId));
    }
}
