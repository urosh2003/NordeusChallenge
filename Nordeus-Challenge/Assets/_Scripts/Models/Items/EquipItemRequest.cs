using System;

[Serializable]
public class EquipItemRequest
{
    public string itemId;
    public EquipItemRequest(string id) { itemId = id; }
}
