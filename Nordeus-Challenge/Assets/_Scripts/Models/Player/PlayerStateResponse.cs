using System;
using System.Collections.Generic;

[Serializable]
public class PlayerStateResponse
{
    public string id;
    public string characterName;
    public int    level;
    public int    currentXp;
    public int    xpToNextLevel;         // 2^(level-1): 1, 2, 4, 8, 16 …
    public int    gold;
    public int    currentHp;             // 0 = full HP (fresh player)
    public int    currentMana;
    public int    currentStamina;
    public CharacterStats   stats;
    public List<string>     knownMoves;    // all moves ever learned
    public List<string>     equippedMoves; // the 4 moves for the next combat
    public int              pendingStatPoints;
    public List<string>     inventory;     // item IDs in the player's bag
    public Equipment        equipment;     // equipped items by slot

    public float XpProgress      => xpToNextLevel > 0 ? (float)currentXp / xpToNextLevel : 0f;
    public bool  HasPendingLevelUp => pendingStatPoints > 0;
}
