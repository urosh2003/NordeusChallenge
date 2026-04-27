using System;
using System.Collections.Generic;

[Serializable]
public class CombatResponse
{
    public string combatId;
    public List<CombatEvent> events;
    public CombatState state;
    public PlayerStateResponse playerState;
}
