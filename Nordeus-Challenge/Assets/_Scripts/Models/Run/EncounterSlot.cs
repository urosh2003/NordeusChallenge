using System;

[Serializable]
public class EncounterSlot
{
    public int index;
    public string enemyDefinitionId;  // e.g. "giantSpider"
    public string enemyName;          // e.g. "Giant Spider"
    public int enemyLevel;            // 1–5, fixed by map position
    public int completions;           // 0 = not yet beaten; >0 = cleared / replayable

    public bool IsCleared    => completions > 0;
    public bool IsReplayable => completions > 0;
}
