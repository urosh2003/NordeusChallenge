using System;
using UnityEngine;

// All fields are flat in JSON regardless of event type.
// Fields irrelevant to a given type will be 0 / null / default.

[Serializable]
public class CombatEvent
{
    public string type;          // raw string matching CombatEventType names

    // MOVE_USED
    public string actorId;
    public string moveId;
    public string targetId;

    // DAMAGE_DEALT / HEAL_RECEIVED / XP_GAINED
    public int amount;
    public string sourceMoveId;

    // STATUS_EFFECT_APPLIED
    public string statType;      // "attack" | "defense" | "magic"
    public int value;            // positive = buff, negative = debuff
    public int duration;

    // COMBAT_ENDED
    public string winnerId;

    // TURN_CHANGED
    public string newTurn;       // "PLAYER" | "ENEMY"

    // LEVEL_UP
    public int newLevel;
    public int pendingStatPoints;

    // RESOURCE_REGEN
    public int manaGained;
    public int staminaGained;

    // RESOURCE_SPENT
    public string costType;    // "mana" | "stamina" | "health"

    // MOVE_LEARNT  →  uses moveId + targetId (already declared above)

    // ITEM_DROPPED
    public string itemId;

    // ENVIRONMENT_EFFECT
    public string environmentId;
    public string resourceType;  // "HEALTH" | "MANA" | "STAMINA"
    public string effectType;    // "GAIN" | "LOSE"

    public CombatEventType EventType
    {
        get
        {
            if (Enum.TryParse(type, out CombatEventType result)) return result;
            return CombatEventType.UNKNOWN;
        }
    }
}

public static class CombatEventFormatter
{
    public static string Format(CombatEvent e)
    {
        return e.EventType switch
        {
            CombatEventType.MOVE_USED             => $"{e.actorId} used {e.moveId} on {e.targetId}.",
            CombatEventType.DAMAGE_DEALT          => $"{e.targetId} took {e.amount} damage.",
            CombatEventType.HEAL_RECEIVED         => $"{e.targetId} healed {e.amount} HP.",
            CombatEventType.STATUS_EFFECT_APPLIED => $"{e.targetId}'s {e.statType} {(e.value >= 0 ? "increased" : "decreased")} by {Mathf.Abs(e.value)} for {e.duration} turns.",
            CombatEventType.TURN_CHANGED          => $"It is now {e.newTurn}'s turn.",
            CombatEventType.COMBAT_ENDED          => $"Combat over! Winner: {e.winnerId}.",
            CombatEventType.XP_GAINED             => $"Gained {e.amount} XP.",
            CombatEventType.MOVE_LEARNT           => $"Learnt new move: {e.moveId}!",
            CombatEventType.LEVEL_UP              => $"Level up! Now level {e.newLevel}. {e.pendingStatPoints} stat points to distribute.",
            CombatEventType.RESOURCE_REGEN        => $"{e.targetId} regenerated {e.manaGained} mana and {e.staminaGained} stamina.",
            CombatEventType.RESOURCE_SPENT        => $"{e.actorId} spent {e.amount} {e.costType}.",
            CombatEventType.ITEM_DROPPED          => $"Item dropped: {e.itemId}!",
            CombatEventType.GOLD_GAINED           => $"Gained {e.amount} gold.",
            CombatEventType.ENVIRONMENT_EFFECT    => $"{e.targetId} {e.effectType.ToLower()}s {e.amount} {e.resourceType.ToLower()} ({e.environmentId}).",
            _                                     => $"[{e.type}]"
        };
    }
}
