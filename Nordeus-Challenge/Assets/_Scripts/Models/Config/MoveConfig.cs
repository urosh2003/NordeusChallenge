using System;
using System.Collections.Generic;

[Serializable]
public class MoveConfig
{
    public string id;
    public string name;
    public string description;
    public MoveCost cost;
    public List<MoveMainEffect> mainEffects;
    public List<MoveStatusEffect> statusEffects;
}

[Serializable]
public class MoveCost
{
    public string costType;    // "mana" | "stamina" | "health" | "none"
    public int costValue;
}

[Serializable]
public class MoveMainEffect
{
    public string type;        // "damage" | "heal" | "steal"
    public string target;      // "self" | "enemy"
    public string resource;    // "health"
    public int baseValue;
    public MoveScaling scaling;     // null when the effect has no scaling
    public string reducedBy;        // "defense" | "magic" | "attack" | null
}

[Serializable]
public class MoveScaling
{
    public string stat;        // "attack" | "magic" | "defense"
    public float multiplier;
}

[Serializable]
public class MoveStatusEffect
{
    public string target;      // "self" | "enemy"
    public string type;        // "attack" | "defense" | "magic"
    public int value;          // positive = buff, negative = debuff
    public int duration;
}
