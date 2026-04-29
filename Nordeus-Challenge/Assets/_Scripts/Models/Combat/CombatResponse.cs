using System;
using System.Collections.Generic;

[Serializable]
public class CombatResponse
{
    public string combatId;
    public List<CombatEvent> events;
    public CombatState state;
    public PlayerStateResponse playerState;
    public string currentTurn;    // "PLAYER" or "ENEMY" while active; null when combat ended
    public string environmentId;  // which environment this combat takes place in
}
