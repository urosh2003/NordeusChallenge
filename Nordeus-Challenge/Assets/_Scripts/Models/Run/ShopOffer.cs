using System;

[Serializable]
public class ShopOffer
{
    public string offerId;
    public string type;    // "ITEM" | "STAT_UPGRADE"
    public int    cost;
    public string itemId;  // ITEM offers only
    public string stat;    // STAT_UPGRADE offers only
    public int    amount;  // STAT_UPGRADE offers only — permanent stat points added
    public bool   purchased;

    public bool IsItem        => type == "ITEM";
    public bool IsStatUpgrade => type == "STAT_UPGRADE";
}
