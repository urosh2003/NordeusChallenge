using System;
using System.Collections.Generic;

[Serializable]
public class RunConfigPlayer
{
    public List<string> knownMoves;
    public List<string> equippedMoves;
    public List<string> inventory;
    public Equipment equipment;
}
