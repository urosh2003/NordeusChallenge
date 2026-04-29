using System;

[Serializable]
public class Equipment
{
    public string mainHand;
    public string offHand;
    public string armor;
    public string gloves;
    public string shoes;
    public string amulet;
    public string ring;

    public bool IsMainHandEmpty => string.IsNullOrEmpty(mainHand);
    public bool IsOffHandEmpty  => string.IsNullOrEmpty(offHand);
    public bool IsArmorEmpty    => string.IsNullOrEmpty(armor);
    public bool IsGlovesEmpty   => string.IsNullOrEmpty(gloves);
    public bool IsShoesEmpty    => string.IsNullOrEmpty(shoes);
    public bool IsAmuletEmpty   => string.IsNullOrEmpty(amulet);
    public bool IsRingEmpty     => string.IsNullOrEmpty(ring);

    public System.Collections.Generic.List<string> GetAllEquipped()
    {
        var list = new System.Collections.Generic.List<string>();
        if (!string.IsNullOrEmpty(mainHand)) list.Add(mainHand);
        if (!string.IsNullOrEmpty(offHand))  list.Add(offHand);
        if (!string.IsNullOrEmpty(armor))    list.Add(armor);
        if (!string.IsNullOrEmpty(gloves))   list.Add(gloves);
        if (!string.IsNullOrEmpty(shoes))    list.Add(shoes);
        if (!string.IsNullOrEmpty(amulet))   list.Add(amulet);
        if (!string.IsNullOrEmpty(ring))     list.Add(ring);
        return list;
    }

    public Equipment Clone() => new Equipment
    {
        mainHand = mainHand, offHand = offHand, armor   = armor,
        gloves   = gloves,   shoes   = shoes,   amulet  = amulet, ring = ring
    };

    public string GetBySlot(EquipmentSlot slot) => slot switch
    {
        EquipmentSlot.MAIN_HAND => mainHand,
        EquipmentSlot.OFF_HAND  => offHand,
        EquipmentSlot.ARMOR     => armor,
        EquipmentSlot.GLOVES    => gloves,
        EquipmentSlot.SHOES     => shoes,
        EquipmentSlot.AMULET    => amulet,
        EquipmentSlot.RING      => ring,
        _                       => null
    };

    public void SetBySlot(EquipmentSlot slot, string itemId)
    {
        switch (slot)
        {
            case EquipmentSlot.MAIN_HAND: mainHand = itemId; break;
            case EquipmentSlot.OFF_HAND:  offHand  = itemId; break;
            case EquipmentSlot.ARMOR:     armor    = itemId; break;
            case EquipmentSlot.GLOVES:    gloves   = itemId; break;
            case EquipmentSlot.SHOES:     shoes    = itemId; break;
            case EquipmentSlot.AMULET:    amulet   = itemId; break;
            case EquipmentSlot.RING:      ring     = itemId; break;
        }
    }

    public static bool SlotAccepts(EquipmentSlot slot, ItemType type) => (slot, type) switch
    {
        (EquipmentSlot.MAIN_HAND, ItemType.ONE_HANDED) => true,
        (EquipmentSlot.MAIN_HAND, ItemType.TWO_HANDED) => true,
        (EquipmentSlot.OFF_HAND,  ItemType.SHIELD)      => true,
        (EquipmentSlot.ARMOR,     ItemType.ARMOR)        => true,
        (EquipmentSlot.GLOVES,    ItemType.GLOVES)       => true,
        (EquipmentSlot.SHOES,     ItemType.SHOES)        => true,
        (EquipmentSlot.AMULET,    ItemType.AMULET)       => true,
        (EquipmentSlot.RING,      ItemType.RING)          => true,
        _                                                => false
    };
}
