using System;
using System.Collections.Generic;

[Serializable]
public class RunNode
{
    public string         id;
    public string         type;              // raw string from JSON: "COMBAT","SHOP","REST_SITE","BOSS"
    public int            row;               // 0 = start (bottom), 9 = boss (top)
    public int            column;            // left-to-right order within the row
    public string         enemyDefinitionId; // COMBAT / BOSS only
    public int            enemyLevel;        // COMBAT / BOSS only
    public string         environmentId;     // COMBAT / BOSS only; null = no environment effects
    public int            completions;
    public List<string>   nextNodeIds;
    public List<ShopOffer> shopOffers;       // SHOP only; populated after first enter

    // JsonUtility cannot deserialize string→enum; use this computed property instead
    public NodeType Type => Enum.TryParse(type, out NodeType t) ? t : NodeType.COMBAT;

    public bool IsCleared  => completions > 0;
    public bool IsBossNode => Type == NodeType.BOSS;
    public bool IsCombat   => Type == NodeType.COMBAT || Type == NodeType.BOSS;
}
