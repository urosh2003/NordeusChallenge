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
    public string gameEventType; // Enum as string for JsonUtility
    
    // Custom parsing for flattened payload since JsonUtility doesn't support Dictionary.
    // We can use a simple manual approach if we know the possible keys, 
    // or keep it simple for now as requested.
    
    public int amount; // Example common field in events
    public string moveId; // Example common field in events
}

[Serializable]
public enum GameEventType
{
    MOVE_USED,
    COMBAT_ENDED,
    DAMAGE_DEALT,
    TURN_CHANGED
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
    public CharacterStats stats;
    public List<string> moves;
}

[Serializable]
public class CharacterStats
{
    public int attack;
    public int defense;
}

public static class GameParser
{
    public static GameResponse Parse(string json)
    {
        return JsonUtility.FromJson<GameResponse>(json);
    }
}
