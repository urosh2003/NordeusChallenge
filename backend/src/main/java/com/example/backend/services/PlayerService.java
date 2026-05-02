package com.example.backend.services;

import com.example.backend.combat.CombatEvent;
import com.example.backend.combat.CombatEventType;
import com.example.backend.dtos.responces.PlayerStateResponse;
import com.example.backend.models.*;
import com.example.backend.repositories.PlayerStateRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import com.example.backend.models.Character;

import java.util.*;

@Service
@Transactional
public class PlayerService {

    private static final String DEFAULT_CHARACTER = "knight"; // fallback for null-runId standalone combats
    private static final int POINTS_PER_LEVEL = 3;

    private final PlayerStateRepository repository;
    private final CharacterLoader characterLoader;
    private final ItemRegistry itemRegistry;

    public PlayerService(PlayerStateRepository repository, CharacterLoader characterLoader,
                         ItemRegistry itemRegistry) {
        this.repository = repository;
        this.characterLoader = characterLoader;
        this.itemRegistry = itemRegistry;
    }

    /**
     * Returns the PlayerState for this run, creating a fresh one if it doesn't exist yet.
     * Pass null only for standalone (test) combats without a run.
     */
    public PlayerState getOrCreatePlayerState(UUID runId) {
        if (runId == null) {
            return repository.findFirstByOrderByIdAsc()
                    .orElseGet(() -> initializeNewPlayer(null, DEFAULT_CHARACTER));
        }
        return repository.findByRunId(runId)
                .orElseGet(() -> initializeNewPlayer(runId, DEFAULT_CHARACTER));
    }

    public void saveState(PlayerState ps) {
        repository.save(ps);
    }

    /** Create a fresh PlayerState for a new run using the chosen class. */
    public PlayerState createPlayerStateForRun(UUID runId, String classId) {
        CharacterDefinition def = characterLoader.getDefinition(classId);
        if (!def.isStartingClass())
            throw new IllegalArgumentException("'" + classId + "' is not a playable starting class");
        return initializeNewPlayer(runId, classId);
    }

    /** Called after the player wins a combat. Saves remaining resources, awards gold. */
    public List<CombatEvent> processVictory(PlayerState playerState, int enemyLevel,
                                            List<String> enemyMoves, String enemyDefinitionId,
                                            Character player) {
        List<CombatEvent> rewardEvents = new ArrayList<>();

        learnMove(playerState, enemyMoves, rewardEvents);
        awardXp(playerState, enemyLevel, rewardEvents);
        dropItem(playerState, enemyDefinitionId, rewardEvents);
        awardGold(playerState, enemyLevel, rewardEvents);

        playerState.setCurrentHp(player.getCurrentHp());
        playerState.setCurrentMana(player.getCurrentMana());
        playerState.setCurrentStamina(player.getCurrentStamina());

        repository.save(playerState);
        return rewardEvents;
    }

    /** Build the player's Character for combat, applying equipped item bonuses and persisted resources. */
    public Character buildCombatCharacter(PlayerState playerState) {
        CharacterStats base = playerState.getStats();
        Equipment eq = playerState.getEquipment();

        int bonusHealth = 0, bonusAttack = 0, bonusDefense = 0, bonusMagic = 0;
        int bonusMaxMana = 0, bonusMaxStamina = 0, bonusManaRegen = 0, bonusStaminaRegen = 0;

        for (String itemId : eq.getAllEquipped()) {
            ItemDefinition item = itemRegistry.getItem(itemId);
            CharacterStats bonus = item.getBonusStats();
            if (bonus != null) {
                bonusHealth   += bonus.getHealth();
                bonusAttack   += bonus.getAttack();
                bonusDefense  += bonus.getDefense();
                bonusMagic    += bonus.getMagic();
            }
            if (item.getPassiveEffects() != null) {
                for (PassiveEffect pe : item.getPassiveEffects()) {
                    switch (pe.getType()) {
                        case BONUS_MAX_MANA      -> bonusMaxMana      += pe.getValue();
                        case BONUS_MAX_STAMINA   -> bonusMaxStamina   += pe.getValue();
                        case BONUS_MANA_REGEN    -> bonusManaRegen    += pe.getValue();
                        case BONUS_STAMINA_REGEN -> bonusStaminaRegen += pe.getValue();
                    }
                }
            }
        }

        CharacterStats effective = new CharacterStats(
                base.getHealth()  + bonusHealth,
                base.getAttack()  + bonusAttack,
                base.getDefense() + bonusDefense,
                base.getMagic()   + bonusMagic
        );

        int maxHp      = effective.getHealth() * 10;
        int maxMana    = effective.getMagic()  * 5  + bonusMaxMana;
        int maxStamina = effective.getAttack() * 5  + bonusMaxStamina;

        // 0 means "not yet set" — use max (fresh player or new run)
        int startHp      = playerState.getCurrentHp()      > 0
                ? Math.min(playerState.getCurrentHp(),      maxHp)      : maxHp;
        int startMana    = playerState.getCurrentMana()    > 0
                ? Math.min(playerState.getCurrentMana(),    maxMana)    : maxMana;
        int startStamina = playerState.getCurrentStamina() > 0
                ? Math.min(playerState.getCurrentStamina(), maxStamina) : maxStamina;

        Character character = new Character(
                "player",
                playerState.getCharacterName(),
                startHp,
                startMana,
                startStamina,
                effective,
                new ArrayList<>(playerState.getEquippedMoves()),
                new ArrayList<>()
        );
        character.setBonusMaxMana(bonusMaxMana);
        character.setBonusMaxStamina(bonusMaxStamina);
        character.setBonusManaRegen(bonusManaRegen);
        character.setBonusStaminaRegen(bonusStaminaRegen);
        return character;
    }

    /** Build a PlayerStateResponse that includes correct max HP/mana/stamina computed from equipment. */
    public PlayerStateResponse buildPlayerStateResponse(PlayerState ps) {
        Character character = buildCombatCharacter(ps);
        return PlayerStateResponse.from(ps, character.getMaxHp(), character.getMaxMana(), character.getMaxStamina());
    }

    public PlayerState equipMoves(UUID runId, List<String> moveIds) {
        if (moveIds == null || moveIds.size() != 4)
            throw new IllegalArgumentException("Exactly 4 moves must be equipped");
        if (new HashSet<>(moveIds).size() != 4)
            throw new IllegalArgumentException("Equipped moves must be unique");

        PlayerState ps = getOrCreatePlayerState(runId);
        for (String moveId : moveIds) {
            if (!ps.getKnownMoves().contains(moveId))
                throw new IllegalArgumentException("Player does not know move: " + moveId);
        }
        ps.setEquippedMoves(new ArrayList<>(moveIds));
        return repository.save(ps);
    }

    /**
     * Apply a complete equipment loadout in one shot (mirrors equipMoves).
     * Any item currently equipped but absent from the new loadout returns to inventory.
     * Any item in the new loadout is removed from inventory.
     * Validates: all items must be in the pool of (current equipment + inventory),
     * each item fits its slot's type, no duplicates, two-handed + shield conflict.
     */
    public PlayerState setEquipment(UUID runId, Equipment incoming) {
        PlayerState ps = getOrCreatePlayerState(runId);
        Equipment current = ps.getEquipment();

        // Pool = everything the player owns (equipped + inventory)
        List<String> pool = new ArrayList<>(ps.getInventory());
        pool.addAll(current.getAllEquipped());

        List<String> slots = incoming.getAllEquipped();

        // No duplicates
        if (slots.stream().distinct().count() != slots.size())
            throw new IllegalArgumentException("Duplicate items in equipment");

        // Every item must come from the pool
        for (String id : slots) {
            if (!pool.contains(id))
                throw new IllegalArgumentException("Item not owned: " + id);
        }

        // Type-slot validation
        validateSlot(incoming.getMainHand(), EquipmentSlot.MAIN_HAND);
        validateSlot(incoming.getOffHand(),  EquipmentSlot.OFF_HAND);
        validateSlot(incoming.getArmor(),    EquipmentSlot.ARMOR);
        validateSlot(incoming.getGloves(),   EquipmentSlot.GLOVES);
        validateSlot(incoming.getShoes(),    EquipmentSlot.SHOES);
        validateSlot(incoming.getAmulet(),   EquipmentSlot.AMULET);
        validateSlot(incoming.getRing(),     EquipmentSlot.RING);

        // Two-handed + offHand conflict
        if (incoming.getMainHand() != null && !incoming.getMainHand().isEmpty()
                && itemRegistry.getItem(incoming.getMainHand()).getItemType() == ItemType.TWO_HANDED
                && incoming.getOffHand() != null && !incoming.getOffHand().isEmpty())
            throw new IllegalArgumentException("Cannot equip an off-hand item with a two-handed weapon");

        // Rebuild inventory = pool minus everything in the new loadout
        List<String> newInventory = new ArrayList<>(pool);
        newInventory.removeAll(slots);

        ps.setInventory(newInventory);
        ps.setEquipment(incoming);
        return repository.save(ps);
    }

    public PlayerState distributeStatPoints(UUID runId, int health, int attack, int defense, int magic) {
        int total = health + attack + defense + magic;
        if (total < 1)
            throw new IllegalArgumentException("Must distribute at least 1 stat point");
        if (health < 0 || attack < 0 || defense < 0 || magic < 0)
            throw new IllegalArgumentException("Stat points cannot be negative");

        PlayerState ps = getOrCreatePlayerState(runId);
        if (ps.getPendingStatPoints() < total)
            throw new IllegalStateException(
                    "Not enough pending stat points: have " + ps.getPendingStatPoints() + ", requested " + total);

        ps.getStats().updateHealth(health);
        ps.getStats().updateAttack(attack);
        ps.getStats().updateDefense(defense);
        ps.getStats().updateMagic(magic);
        ps.setPendingStatPoints(ps.getPendingStatPoints() - total);
        return repository.save(ps);
    }

    // ── private helpers ──────────────────────────────────────────────────────

    private void learnMove(PlayerState ps, List<String> enemyMoves, List<CombatEvent> events) {
        enemyMoves.stream()
                .filter(m -> !ps.getKnownMoves().contains(m))
                .findFirst()
                .ifPresent(moveId -> {
                    ps.getKnownMoves().add(moveId);
                    events.add(CombatEvent.of(CombatEventType.MOVE_LEARNT)
                            .with("targetId", "player")
                            .with("moveId", moveId));
                });
    }

    private void awardXp(PlayerState ps, int xpGained, List<CombatEvent> events) {
        ps.setCurrentXp(ps.getCurrentXp() + xpGained);
        events.add(CombatEvent.of(CombatEventType.XP_GAINED)
                .with("targetId", "player")
                .with("amount", xpGained));

        while (ps.getCurrentXp() >= ps.getXpToNextLevel()) {
            ps.setCurrentXp(ps.getCurrentXp() - ps.getXpToNextLevel());
            ps.setLevel(ps.getLevel() + 1);
            ps.setPendingStatPoints(ps.getPendingStatPoints() + POINTS_PER_LEVEL);
            events.add(CombatEvent.of(CombatEventType.LEVEL_UP)
                    .with("targetId", "player")
                    .with("newLevel", ps.getLevel())
                    .with("pendingStatPoints", ps.getPendingStatPoints()));
        }
    }

    private void awardGold(PlayerState ps, int enemyLevel, List<CombatEvent> events) {
        int goldMin = enemyLevel * 10;
        int goldMax = enemyLevel * 20;
        int goldGained = goldMin + new Random().nextInt(goldMax - goldMin + 1);
        ps.setGold(ps.getGold() + goldGained);
        events.add(CombatEvent.of(CombatEventType.GOLD_GAINED)
                .with("targetId", "player")
                .with("amount", goldGained));
    }

    private void dropItem(PlayerState ps, String enemyDefinitionId, List<CombatEvent> events) {
        if (enemyDefinitionId == null) return;
        CharacterDefinition def = characterLoader.getDefinition(enemyDefinitionId);
        List<String> drops = def.getPossibleDrops();
        if (drops == null || drops.isEmpty()) return;

        String itemId = drops.get(new Random().nextInt(drops.size()));
        ps.getInventory().add(itemId);
        events.add(CombatEvent.of(CombatEventType.ITEM_DROPPED)
                .with("itemId", itemId));
    }

    private PlayerState initializeNewPlayer(UUID runId, String classId) {
        var def = characterLoader.getDefinition(classId);
        var initialChar = characterLoader.createCharacter(classId, 1);

        PlayerState ps = new PlayerState();
        ps.setRunId(runId);
        ps.setCharacterId(initialChar.getId());
        ps.setCharacterName(def.getName());
        ps.setLevel(1);
        ps.setCurrentXp(0);
        ps.setGold(0);
        ps.setStats(new CharacterStats(
                initialChar.getStats().getHealth(),
                initialChar.getStats().getAttack(),
                initialChar.getStats().getDefense(),
                initialChar.getStats().getMagic()
        ));
        ps.setKnownMoves(new ArrayList<>(initialChar.getMoves()));
        ps.setEquippedMoves(new ArrayList<>(initialChar.getMoves()));
        ps.setInventory(new ArrayList<>());
        ps.setEquipment(new Equipment());
        return repository.save(ps);
    }

    // ── slot helpers ─────────────────────────────────────────────────────────

    private void validateSlot(String itemId, EquipmentSlot slot) {
        if (itemId == null || itemId.isEmpty()) return;
        ItemDefinition item = itemRegistry.getItem(itemId);
        EquipmentSlot expected = slotFor(item.getItemType());
        if (expected != slot)
            throw new IllegalArgumentException(
                    "Item '" + itemId + "' (type " + item.getItemType() + ") does not fit slot " + slot);
    }

    private EquipmentSlot slotFor(ItemType type) {
        return switch (type) {
            case ONE_HANDED, TWO_HANDED -> EquipmentSlot.MAIN_HAND;
            case SHIELD                 -> EquipmentSlot.OFF_HAND;
            case ARMOR                  -> EquipmentSlot.ARMOR;
            case GLOVES                 -> EquipmentSlot.GLOVES;
            case SHOES                  -> EquipmentSlot.SHOES;
            case AMULET                 -> EquipmentSlot.AMULET;
            case RING                   -> EquipmentSlot.RING;
        };
    }

    private String getSlot(Equipment eq, EquipmentSlot slot) {
        return switch (slot) {
            case MAIN_HAND -> eq.getMainHand();
            case OFF_HAND  -> eq.getOffHand();
            case ARMOR     -> eq.getArmor();
            case GLOVES    -> eq.getGloves();
            case SHOES     -> eq.getShoes();
            case AMULET    -> eq.getAmulet();
            case RING      -> eq.getRing();
        };
    }

    private void setSlot(Equipment eq, EquipmentSlot slot, String itemId) {
        switch (slot) {
            case MAIN_HAND -> eq.setMainHand(itemId);
            case OFF_HAND  -> eq.setOffHand(itemId);
            case ARMOR     -> eq.setArmor(itemId);
            case GLOVES    -> eq.setGloves(itemId);
            case SHOES     -> eq.setShoes(itemId);
            case AMULET    -> eq.setAmulet(itemId);
            case RING      -> eq.setRing(itemId);
        }
    }

    private EquipmentSlot findSlot(Equipment eq, String itemId) {
        for (EquipmentSlot slot : EquipmentSlot.values()) {
            if (itemId.equals(getSlot(eq, slot))) return slot;
        }
        return null;
    }

}
