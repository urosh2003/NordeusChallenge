using System;

[Serializable]
public class EquipEquipmentRequest
{
    public string mainHand;
    public string offHand;
    public string armor;
    public string gloves;
    public string shoes;
    public string amulet;
    public string ring;

    public EquipEquipmentRequest(Equipment eq)
    {
        mainHand = eq?.mainHand;
        offHand  = eq?.offHand;
        armor    = eq?.armor;
        gloves   = eq?.gloves;
        shoes    = eq?.shoes;
        amulet   = eq?.amulet;
        ring     = eq?.ring;
    }
}
