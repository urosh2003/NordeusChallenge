using System;

public enum PassiveEffectType
{
    BONUS_MAX_MANA, BONUS_MAX_STAMINA, BONUS_MANA_REGEN, BONUS_STAMINA_REGEN
}

[Serializable]
public class PassiveEffect
{
    public string type;
    public int value;

    public PassiveEffectType EffectType =>
        Enum.TryParse(type, out PassiveEffectType t) ? t : PassiveEffectType.BONUS_MAX_MANA;
}
