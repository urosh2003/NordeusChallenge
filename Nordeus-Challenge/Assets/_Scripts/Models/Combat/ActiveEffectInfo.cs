using System;

[Serializable]
public class ActiveEffectInfo
{
    public string effectType;  // "attack" | "defense" | "magic"
    public int    value;       // positive = buff, negative = debuff
    public int    duration;    // turns remaining
}
