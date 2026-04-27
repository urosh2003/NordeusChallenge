using System;

[Serializable]
public class MoveRequest
{
    public string moveId;
    public MoveRequest(string moveId) { this.moveId = moveId; }
}
