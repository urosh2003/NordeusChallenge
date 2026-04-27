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
}
