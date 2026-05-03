package com.example.backend.services.runs;

import com.example.backend.services.players.PlayerService;
import com.example.backend.services.characters.CharacterLoader;
import com.example.backend.services.combats.CombatSessionService;
import com.example.backend.models.moves.MoveDefinition;
import com.example.backend.services.items.ItemRegistry;
import com.example.backend.services.moves.MoveRegistry;
import com.example.backend.dtos.responces.*;
import com.example.backend.models.environments.EnvironmentDefinition;
import com.example.backend.models.characters.CharacterDefinition;
import com.example.backend.models.characters.CharacterStats;
import com.example.backend.models.combats.CombatInstance;
import com.example.backend.models.combats.CombatStatus;
import com.example.backend.models.encounters.EncounterNode;
import com.example.backend.models.encounters.EncounterType;
import com.example.backend.models.encounters.ShopOffer;
import com.example.backend.models.items.ItemDefinition;
import com.example.backend.models.players.PlayerState;
import com.example.backend.models.runs.Run;
import com.example.backend.models.runs.RunStatus;
import com.example.backend.repositories.CombatInstanceRepository;
import com.example.backend.repositories.RunRepository;
import com.example.backend.services.environments.EnvironmentLoader;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import com.example.backend.models.characters.Character;

import java.util.*;
import java.util.stream.Collectors;

@Service
@Transactional
public class RunService {

    private static final int GRAPH_HEIGHT = 10;
    private static final int BOSS_ROW = GRAPH_HEIGHT - 1;

    private final RunRepository runRepo;
    private final CombatInstanceRepository combatRepo;
    private final CombatSessionService combatSessionService;
    private final CharacterLoader characterLoader;
    private final MoveRegistry moveRegistry;
    private final ItemRegistry itemRegistry;
    private final PlayerService playerService;
    private final ShopConfig shopConfig;
    private final EnvironmentLoader environmentLoader;

    public RunService(RunRepository runRepo, CombatInstanceRepository combatRepo,
                      CombatSessionService combatSessionService, CharacterLoader characterLoader,
                      MoveRegistry moveRegistry, ItemRegistry itemRegistry,
                      PlayerService playerService, ShopConfig shopConfig,
                      EnvironmentLoader environmentLoader) {
        this.runRepo = runRepo;
        this.combatRepo = combatRepo;
        this.combatSessionService = combatSessionService;
        this.characterLoader = characterLoader;
        this.moveRegistry = moveRegistry;
        this.itemRegistry = itemRegistry;
        this.playerService = playerService;
        this.shopConfig = shopConfig;
        this.environmentLoader = environmentLoader;
    }

    public RunResponse createRun(String classId) {
        Random rng = new Random();
        List<EncounterNode> nodes = generateGraph(rng);

        String startNodeId = nodes.stream()
                .filter(n -> n.getRow() == 0)
                .findFirst()
                .orElseThrow()
                .getId();

        Run run = new Run();
        run.setNodes(nodes);
        run.setCurrentNodeId(startNodeId);
        run = runRepo.save(run);
        playerService.createPlayerStateForRun(run.getId(), classId);
        return RunResponse.from(run);
    }

    @Transactional(readOnly = true)
    public RunResponse getRun(UUID runId) {
        Run run = loadRun(runId);
        UUID activeCombatId = combatRepo.findByRunIdAndStatus(runId, CombatStatus.ACTIVE)
                .map(CombatInstance::getId)
                .orElse(null);
        return RunResponse.from(run, activeCombatId);
    }

    /**
     * Start combat for a COMBAT or BOSS node.
     * - nodeId == currentNodeId: fight (or replay) the current node.
     * - nodeId ∈ currentNode.nextNodeIds AND currentNode.completions > 0: advance then fight.
     */
    public CombatResponse startCombat(UUID runId, String nodeId) {
        Run run = loadRun(runId);

        if (run.getStatus() == RunStatus.COMPLETED)
            throw new IllegalStateException("Run is already completed");
        if (combatRepo.existsByRunIdAndStatus(runId, CombatStatus.ACTIVE))
            throw new IllegalStateException("A combat is already in progress for this run");

        EncounterNode current = findNode(run, run.getCurrentNodeId());
        boolean isCurrent = nodeId.equals(run.getCurrentNodeId());
        boolean isNextNode = current.getNextNodeIds().contains(nodeId) && current.getCompletions() > 0;

        if (!isCurrent && !isNextNode)
            throw new IllegalStateException("Node " + nodeId + " is not accessible from the current position");

        if (isNextNode) {
            run.setCurrentNodeId(nodeId);
            runRepo.save(run);
        }

        EncounterNode node = findNode(run, nodeId);
        if (node.getType() != EncounterType.COMBAT && node.getType() != EncounterType.BOSS)
            throw new IllegalStateException("Node " + nodeId + " is not a combat node — use /enter instead");

        return combatSessionService.createCombat(
                node.getEnemyDefinitionId(), node.getEnemyLevel(), runId, nodeId, node.getEnvironmentId());
    }

    /**
     * Enter a SHOP or REST_SITE node.
     * On the first visit: generates shop offers (SHOP) or heals the player (REST_SITE) and increments completions.
     * Subsequent calls return current state without re-applying effects.
     */
    public NodeEnterResponse enterNode(UUID runId, String nodeId) {
        Run run = loadRun(runId);

        if (run.getStatus() == RunStatus.COMPLETED)
            throw new IllegalStateException("Run is already completed");

        EncounterNode current = findNode(run, run.getCurrentNodeId());
        boolean isCurrent = nodeId.equals(run.getCurrentNodeId());
        boolean isNextNode = current.getNextNodeIds().contains(nodeId) && current.getCompletions() > 0;

        if (!isCurrent && !isNextNode)
            throw new IllegalStateException("Node " + nodeId + " is not accessible from the current position");

        if (isNextNode) {
            run.setCurrentNodeId(nodeId);
        }

        EncounterNode node = findNode(run, nodeId);
        if (node.getType() != EncounterType.SHOP && node.getType() != EncounterType.REST_SITE)
            throw new IllegalStateException("Node " + nodeId + " is not a shop or rest site — use /start for combat");

        PlayerState ps = playerService.getOrCreatePlayerState(runId);

        if (node.getCompletions() == 0) {
            if (node.getType() == EncounterType.SHOP) {
                node.setShopOffers(generateShopOffers(new Random()));
            } else {
                applyRest(ps);
            }
            node.incrementCompletions();
        }

        playerService.saveState(ps);
        runRepo.save(run);

        return new NodeEnterResponse(RunResponse.from(run), playerService.buildPlayerStateResponse(ps));
    }

    /**
     * Buy an offer from the current shop node.
     */
    public PlayerStateResponse buyOffer(UUID runId, String offerId) {
        Run run = loadRun(runId);
        EncounterNode node = findNode(run, run.getCurrentNodeId());

        if (node.getType() != EncounterType.SHOP)
            throw new IllegalStateException("Current node is not a shop");
        if (node.getCompletions() == 0)
            throw new IllegalStateException("You must enter the shop before buying");

        ShopOffer offer = node.getShopOffers().stream()
                .filter(o -> o.getOfferId().equals(offerId))
                .findFirst()
                .orElseThrow(() -> new IllegalArgumentException("Offer not found: " + offerId));

        if (offer.isPurchased())
            throw new IllegalStateException("Offer already purchased");

        PlayerState ps = playerService.getOrCreatePlayerState(runId);
        if (ps.getGold() < offer.getCost())
            throw new IllegalStateException("Insufficient gold (have " + ps.getGold() + ", need " + offer.getCost() + ")");

        ps.setGold(ps.getGold() - offer.getCost());

        if (offer.getType() == ShopOffer.OfferType.ITEM) {
            ps.getInventory().add(offer.getItemId());
        } else {
            switch (offer.getStat()) {
                case "health"  -> ps.getStats().updateHealth(offer.getAmount());
                case "attack"  -> ps.getStats().updateAttack(offer.getAmount());
                case "defense" -> ps.getStats().updateDefense(offer.getAmount());
                case "magic"   -> ps.getStats().updateMagic(offer.getAmount());
                default -> throw new IllegalArgumentException("Unknown stat: " + offer.getStat());
            }
        }

        offer.setPurchased(true);
        playerService.saveState(ps);
        runRepo.save(run);

        return playerService.buildPlayerStateResponse(ps);
    }

    /**
     * Sell an item from inventory at the current shop. Sell price = 40% of item cost, minimum 5 gold.
     */
    public PlayerStateResponse sellItem(UUID runId, String itemId) {
        Run run = loadRun(runId);
        EncounterNode node = findNode(run, run.getCurrentNodeId());

        if (node.getType() != EncounterType.SHOP)
            throw new IllegalStateException("Current node is not a shop");

        PlayerState ps = playerService.getOrCreatePlayerState(runId);
        if (!ps.getInventory().contains(itemId))
            throw new IllegalArgumentException("Item not in inventory: " + itemId);

        ItemDefinition item = itemRegistry.getItem(itemId);
        int sellPrice = Math.max(5, item.getCost() * shopConfig.getSellPricePercent() / 100);

        ps.getInventory().remove(itemId);
        ps.setGold(ps.getGold() + sellPrice);

        playerService.saveState(ps);
        return playerService.buildPlayerStateResponse(ps);
    }

    @Transactional(readOnly = true)
    public boolean hasActiveCombat(UUID runId) {
        return combatRepo.existsByRunIdAndStatus(runId, CombatStatus.ACTIVE);
    }

    @Transactional(readOnly = true)
    public RunConfigResponse getRunConfig(UUID runId) {
        Run run = loadRun(runId);
        PlayerState playerState = playerService.getOrCreatePlayerState(runId);

        Set<String> allMoveIds = new LinkedHashSet<>();
        Set<String> allItemIds = new LinkedHashSet<>();

        List<EncounterConfig> encounterConfigs = new ArrayList<>();
        for (EncounterNode node : run.getNodes()) {
            EncounterConfig config;

            if (node.getType() == EncounterType.COMBAT || node.getType() == EncounterType.BOSS) {
                CharacterDefinition def = characterLoader.getDefinition(node.getEnemyDefinitionId());
                Character enemy = characterLoader.createCharacter(node.getEnemyDefinitionId(), node.getEnemyLevel());
                CharacterStats stats = enemy.getStats();
                List<String> drops = def.getPossibleDrops() != null ? def.getPossibleDrops() : List.of();

                allMoveIds.addAll(def.getMoves());
                allItemIds.addAll(drops);

                config = new EncounterConfig(
                        node.getId(), node.getRow(), node.getColumn(), node.getType(),
                        node.getEnemyDefinitionId(), node.getEnemyName(), node.getEnemyLevel(),
                        stats, enemy.getMaxHp(), enemy.getMaxMana(), enemy.getManaPerTurn(),
                        enemy.getMaxStamina(), enemy.getStaminaPerTurn(),
                        def.getMoves(), drops, node.getCompletions(), node.getNextNodeIds()
                );
            } else {
                config = new EncounterConfig(
                        node.getId(), node.getRow(), node.getColumn(), node.getType(),
                        null, null, 0,
                        null, 0, 0, 0, 0, 0,
                        List.of(), List.of(), node.getCompletions(), node.getNextNodeIds()
                );
            }
            encounterConfigs.add(config);
        }

        allMoveIds.addAll(playerState.getKnownMoves());
        allItemIds.addAll(playerState.getInventory());
        allItemIds.addAll(playerState.getEquipment().getAllEquipped());
        allItemIds.addAll(itemRegistry.getAll().keySet());

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
                playerState.getEquipment(),
                playerState.getCharacterId()
        );

        Map<String, EnvironmentDefinition> environments = environmentLoader.getAll().stream()
                .collect(Collectors.toMap(EnvironmentDefinition::getId, e -> e,
                        (a, b) -> a, LinkedHashMap::new));

        return new RunConfigResponse(run.getId(), encounterConfigs, moves, items, player, environments);
    }

    // ── Shop helpers ─────────────────────────────────────────────────────────

    private List<ShopOffer> generateShopOffers(Random rng) {
        List<ShopOffer> offers = new ArrayList<>();
        String[] statNames = {"health", "attack", "defense", "magic"};

        List<String> statsPool = new ArrayList<>(List.of(statNames));
        Collections.shuffle(statsPool, rng);
        int statCount = Math.min(shopConfig.getStatOfferCount(), statsPool.size());
        for (int i = 0; i < statCount; i++) {
            String stat = statsPool.get(i);
            int range = shopConfig.getStatAmountMax() - shopConfig.getStatAmountMin();
            int amount = shopConfig.getStatAmountMin() + (range > 0 ? rng.nextInt(range + 1) : 0);
            int cost = amount * shopConfig.getStatCostPerPoint();
            offers.add(new ShopOffer(UUID.randomUUID().toString(),
                    ShopOffer.OfferType.STAT_UPGRADE, cost, null, stat, amount, false));
        }

        List<ItemDefinition> purchasable = itemRegistry.getAll().values().stream()
                .filter(ItemDefinition::isPurchasable)
                .collect(Collectors.toList());
        Collections.shuffle(purchasable, rng);
        int itemCount = Math.min(shopConfig.getItemOfferCount(), purchasable.size());
        for (int i = 0; i < itemCount; i++) {
            ItemDefinition item = purchasable.get(i);
            offers.add(new ShopOffer(UUID.randomUUID().toString(),
                    ShopOffer.OfferType.ITEM, item.getCost(), item.getId(), null, 0, false));
        }

        return offers;
    }

    private void applyRest(PlayerState ps) {
        Character combatChar = playerService.buildCombatCharacter(ps);
        ps.setCurrentHp(combatChar.getMaxHp());
        ps.setCurrentMana(combatChar.getMaxMana());
        ps.setCurrentStamina(combatChar.getMaxStamina());
    }

    // ── Graph generation ─────────────────────────────────────────────────────

    private List<EncounterNode> generateGraph(Random rng) {
        List<List<EncounterNode>> rows = new ArrayList<>();
        List<CharacterDefinition> enemies = characterLoader.getEnemies();
        List<CharacterDefinition> bosses = characterLoader.getBosses();
        if (enemies.isEmpty()) {
            throw new IllegalStateException("No enemy character definitions configured");
        }
        List<String> environmentIds = environmentLoader.getAll().stream()
                .map(EnvironmentDefinition::getId)
                .collect(Collectors.toList());

        for (int r = 0; r < GRAPH_HEIGHT; r++) {
            int width = (r == 0 || r == BOSS_ROW) ? 1 : 3 + rng.nextInt(2);
            List<EncounterNode> row = new ArrayList<>();
            for (int col = 0; col < width; col++) {
                EncounterType type = pickEncounterType(r, rng);
                String defId = null, defName = null, envId = null;

                if (type == EncounterType.COMBAT) {
                    CharacterDefinition enemy = enemies.get(rng.nextInt(enemies.size()));
                    defId = enemy.getId();
                    defName = enemy.getName();
                    envId = environmentIds.get(rng.nextInt(environmentIds.size()));
                }
                if (type == EncounterType.BOSS) {
                    CharacterDefinition enemy = bosses.get(rng.nextInt(bosses.size()));
                    defId = enemy.getId();
                    defName = enemy.getName();
                    envId = environmentIds.get(rng.nextInt(environmentIds.size()));
                }

                EncounterNode node = new EncounterNode(
                        UUID.randomUUID().toString(),
                        r, col, type,
                        defId, defName,
                        r + 1,
                        envId,
                        0,
                        new ArrayList<>(),
                        new ArrayList<>()
                );
                row.add(node);
            }
            rows.add(row);
        }

        for (int r = 0; r < GRAPH_HEIGHT - 1; r++) {
            connectRows(rows.get(r), rows.get(r + 1), rng);
        }

        return rows.stream().flatMap(List::stream).collect(Collectors.toList());
    }

    private EncounterType pickEncounterType(int row, Random rng) {
        if (row == 0) return EncounterType.COMBAT;
        if (row == BOSS_ROW) return EncounterType.BOSS;

        Map<String, Integer> weights = shopConfig.getEncounterTypeWeights();
        int total = weights.values().stream().mapToInt(Integer::intValue).sum();
        int roll = rng.nextInt(total);
        int cumulative = 0;
        for (Map.Entry<String, Integer> entry : weights.entrySet()) {
            cumulative += entry.getValue();
            if (roll < cumulative) {
                return EncounterType.valueOf(entry.getKey());
            }
        }
        return EncounterType.COMBAT;
    }

    private void connectRows(List<EncounterNode> fromRow, List<EncounterNode> toRow, Random rng) {
        int n = fromRow.size();
        int m = toRow.size();
        boolean[] covered = new boolean[m];
        int[] baseTarget = new int[n];

        for (int i = 0; i < n; i++) {
            int j = (n == 1) ? 0 : (int) Math.round((double) i * (m - 1) / (n - 1));
            baseTarget[i] = j;
            fromRow.get(i).getNextNodeIds().add(toRow.get(j).getId());
            covered[j] = true;
        }

        for (int j = 0; j < m; j++) {
            if (!covered[j]) {
                int i = (m == 1) ? 0 : Math.min((int) Math.round((double) j * (n - 1) / (m - 1)), n - 1);
                fromRow.get(i).getNextNodeIds().add(toRow.get(j).getId());
            }
        }

        for (int i = 0; i < n; i++) {
            if (rng.nextDouble() < 0.4) {
                int adj = baseTarget[i] + (rng.nextBoolean() ? 1 : -1);
                if (adj >= 0 && adj < m) {
                    String adjId = toRow.get(adj).getId();
                    if (!fromRow.get(i).getNextNodeIds().contains(adjId)) {
                        fromRow.get(i).getNextNodeIds().add(adjId);
                    }
                }
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private EncounterNode findNode(Run run, String nodeId) {
        return run.getNodes().stream()
                .filter(n -> n.getId().equals(nodeId))
                .findFirst()
                .orElseThrow(() -> new IllegalArgumentException("Node not found: " + nodeId));
    }

    private Run loadRun(UUID runId) {
        return runRepo.findById(runId)
                .orElseThrow(() -> new IllegalArgumentException("Run not found: " + runId));
    }
}
