using System;
using System.Collections.Generic;

[Serializable]
public class PlayerStateResponse
{
    public string id;
    public string characterName;
    public int level;
    public int currentXp;
    public int xpToNextLevel;          // 2^(level-1): 1, 2, 4, 8, 16 …
    public CharacterStats stats;
    public List<string> knownMoves;    // all moves ever learned
    public List<string> equippedMoves; // the 4 moves selected for the next combat
    public int pendingStatPoints;      // unspent level-up points (3 per level)
    public List<string> inventory;     // item IDs in the player's bag
    public Equipment equipment;        // currently equipped items by slot

    public float XpProgress      => xpToNextLevel > 0 ? (float)currentXp / xpToNextLevel : 0f;
    public bool HasPendingLevelUp => pendingStatPoints > 0;
}
