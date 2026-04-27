package com.example.backend.services;

import com.example.backend.combat.CombatEvent;
import com.example.backend.combat.CombatEventType;
import com.example.backend.models.*;
import com.example.backend.repositories.PlayerStateRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import com.example.backend.models.Character;

import java.util.*;

@Service
@Transactional
public class PlayerService {

    private static final String DEFAULT_CHARACTER = "knight";
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

    public PlayerState getOrCreatePlayerState() {
        return repository.findFirstByOrderByIdAsc()
                .orElseGet(this::initializeNewPlayer);
    }

    /** Called after the player wins a combat. Returns reward events to include in the response. */
    public List<CombatEvent> processVictory(PlayerState playerState, int enemyLevel,
                                            List<String> enemyMoves, String enemyDefinitionId) {
        List<CombatEvent> rewardEvents = new ArrayList<>();

        learnMove(playerState, enemyMoves, rewardEvents);
        awardXp(playerState, enemyLevel, rewardEvents);
        dropItem(playerState, enemyDefinitionId, rewardEvents);

        repository.save(playerState);
        return rewardEvents;
    }

    /** Build the player's Character for combat, applying equipped item bonuses. */
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

        Character character = new Character(
                "player",
                playerState.getCharacterName(),
                effective.getHealth() * 10,
                effective.getMagic()  * 5  + bonusMaxMana,
                effective.getAttack() * 5  + bonusMaxStamina,
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

    /** Update which 4 moves are equipped for the next combat. */
    public PlayerState equipMoves(List<String> moveIds) {
        if (moveIds == null || moveIds.size() != 4) {
            throw new IllegalArgumentException("Exactly 4 moves must be equipped");
        }
        if (new HashSet<>(moveIds).size() != 4) {
            throw new IllegalArgumentException("Equipped moves must be unique");
        }

        PlayerState playerState = getOrCreatePlayerState();
        for (String moveId : moveIds) {
            if (!playerState.getKnownMoves().contains(moveId)) {
                throw new IllegalArgumentException("Player does not know move: " + moveId);
            }
        }

        playerState.setEquippedMoves(new ArrayList<>(moveIds));
        return repository.save(playerState);
    }

    /** Move an item from inventory into the appropriate equipment slot. */
    public PlayerState equipItem(String itemId) {
        PlayerState ps = getOrCreatePlayerState();
        ItemDefinition item = itemRegistry.getItem(itemId);

        if (!ps.getInventory().contains(itemId)) {
            throw new IllegalArgumentException("Item not in inventory: " + itemId);
        }

        Equipment eq = ps.getEquipment();

        if (item.getItemType() == ItemType.TWO_HANDED) {
            // TWO_HANDED takes mainHand; off-hand must be cleared
            String displaced = eq.getOffHand();
            if (displaced != null) {
                ps.getInventory().add(displaced);
                eq.setOffHand(null);
            }
            returnToInventory(ps, eq, EquipmentSlot.MAIN_HAND);
            eq.setMainHand(itemId);

        } else if (item.getItemType() == ItemType.SHIELD) {
            // SHIELD always goes off-hand; if a TWO_HANDED is in main-hand, move it to inventory
            displaceTwoHandedIfPresent(ps, eq);
            returnToInventory(ps, eq, EquipmentSlot.OFF_HAND);
            eq.setOffHand(itemId);

        } else {
            // ONE_HANDED goes to main-hand by default; all other slots are unique
            EquipmentSlot slot = slotFor(item.getItemType());
            if (slot == EquipmentSlot.MAIN_HAND) {
                // If a TWO_HANDED is already there the off-hand is already clear, just replace
                returnToInventory(ps, eq, EquipmentSlot.MAIN_HAND);
                eq.setMainHand(itemId);
            } else {
                returnToInventory(ps, eq, slot);
                setSlot(eq, slot, itemId);
            }
        }

        ps.getInventory().remove(itemId);
        return repository.save(ps);
    }

    /** Move an equipped item back into inventory. */
    public PlayerState unequipItem(String itemId) {
        PlayerState ps = getOrCreatePlayerState();
        Equipment eq = ps.getEquipment();

        EquipmentSlot slot = findSlot(eq, itemId);
        if (slot == null) {
            throw new IllegalArgumentException("Item is not equipped: " + itemId);
        }

        setSlot(eq, slot, null);
        ps.getInventory().add(itemId);
        return repository.save(ps);
    }

    /** Spend 3 pending stat points from one level-up. */
    public PlayerState distributeStatPoints(int health, int attack, int defense, int magic) {
        int total = health + attack + defense + magic;
        if (total != POINTS_PER_LEVEL) {
            throw new IllegalArgumentException(
                    "Must distribute exactly " + POINTS_PER_LEVEL + " points, got " + total);
        }
        if (health < 0 || attack < 0 || defense < 0 || magic < 0) {
            throw new IllegalArgumentException("Stat points cannot be negative");
        }

        PlayerState playerState = getOrCreatePlayerState();
        if (playerState.getPendingStatPoints() < POINTS_PER_LEVEL) {
            throw new IllegalStateException("No pending stat points to distribute");
        }

        playerState.getStats().updateHealth(health);
        playerState.getStats().updateAttack(attack);
        playerState.getStats().updateDefense(defense);
        playerState.getStats().updateMagic(magic);
        playerState.setPendingStatPoints(playerState.getPendingStatPoints() - POINTS_PER_LEVEL);

        return repository.save(playerState);
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

    private PlayerState initializeNewPlayer() {
        var def = characterLoader.getDefinition(DEFAULT_CHARACTER);
        var initialChar = characterLoader.createCharacter("player", DEFAULT_CHARACTER, 1);

        PlayerState ps = new PlayerState();
        ps.setCharacterDefinitionId(def.getId());
        ps.setCharacterName(def.getName());
        ps.setLevel(1);
        ps.setCurrentXp(0);
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

    /** If current slot occupant exists, push it back to inventory. */
    private void returnToInventory(PlayerState ps, Equipment eq, EquipmentSlot slot) {
        String current = getSlot(eq, slot);
        if (current != null) {
            ps.getInventory().add(current);
            setSlot(eq, slot, null);
        }
    }

    /** If mainHand holds a TWO_HANDED item, move it back to inventory. */
    private void displaceTwoHandedIfPresent(PlayerState ps, Equipment eq) {
        String mainHandId = eq.getMainHand();
        if (mainHandId != null && itemRegistry.getItem(mainHandId).getItemType() == ItemType.TWO_HANDED) {
            ps.getInventory().add(mainHandId);
            eq.setMainHand(null);
        }
    }
}
