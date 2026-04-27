using System;

[Serializable]
public class ItemBonusStats
{
    public int health;
    public int attack;
    public int defense;
    public int magic;
}

[Serializable]
public class ItemDefinition
{
    public string id;
    public string name;
    public string description;
    public string itemType;
    public ItemBonusStats bonusStats;
    public PassiveEffect[] passiveEffects;

    public ItemType ItemType =>
        Enum.TryParse(itemType, out ItemType t) ? t : ItemType.ONE_HANDED;
}
