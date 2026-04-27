using System;
using System.Collections.Generic;

[Serializable]
public class EquipMovesRequest
{
    public List<string> moveIds;
    public EquipMovesRequest(List<string> moveIds) { this.moveIds = moveIds; }
}
