using UnityEngine;

[CreateAssetMenu(fileName = "NewItemSO", menuName = "Game Data/Item Data", order = 3)]
public class ItemSO : ScriptableObject
{
    public string itemId;
    public Sprite icon;
}
