using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameResponse
{
    public string gameId; // Guid as string for JsonUtility
    public List<GameEvent> events;
    public CombatState state;
}

[Serializable]
public class GameEvent
{
    public string type;         // always present — matches GameEventType enum names
    // MOVE_USED
    public string actorId;
    public string moveId;
    public string targetId;
    // DAMAGE_DEALT / HEAL_RECEIVED
    public int amount;
    public string sourceMoveId;
    // STATUS_EFFECT_APPLIED
    public string statType;     // "attack" | "defense" | "magic"
    public int value;           // positive = buff, negative = debuff
    public int duration;
    // COMBAT_ENDED
    public string winnerId;
    // TURN_CHANGED
    public string newTurn;      // "PLAYER" | "ENEMY"
}

[Serializable]
public enum GameEventType
{
    MOVE_USED,
    COMBAT_ENDED,
    DAMAGE_DEALT,
    TURN_CHANGED,
    HEAL_RECEIVED,
    STATUS_EFFECT_APPLIED
}

[Serializable]
public class CombatState
{
    public Character player;
    public Character enemy;
}

[Serializable]
public class Character
{
    public string id;
    public string name;
    public int currentHp;
    public int maxHp;
    public int currentMana;
    public int maxMana;
    public int manaPerTurn;
    public CharacterStats stats;
    public List<string> moves;
}

[Serializable]
public class CharacterStats
{
    public int health;
    public int attack;
    public int defense;
    public int magic;
}

public static class GameParser
{
    public static GameResponse Parse(string json)
    {
        return JsonUtility.FromJson<GameResponse>(json);
    }

    public static string FormatEvent(GameEvent e)
    {
        return e.type switch
        {
            "MOVE_USED"             => $"{e.actorId} used {e.moveId} on {e.targetId}.",
            "DAMAGE_DEALT"          => $"{e.targetId} took {e.amount} damage.",
            "HEAL_RECEIVED"         => $"{e.targetId} healed for {e.amount} HP.",
            "STATUS_EFFECT_APPLIED" => $"{e.targetId}'s {e.statType} {(e.value >= 0 ? "increased" : "decreased")} by {Mathf.Abs(e.value)} for {e.duration} turns.",
            "TURN_CHANGED"          => $"Turn changed — it is now {e.newTurn}'s turn.",
            "COMBAT_ENDED"          => $"Combat over! Winner: {e.winnerId}.",
            _                       => $"[{e.type}]"
        };
    }
}
