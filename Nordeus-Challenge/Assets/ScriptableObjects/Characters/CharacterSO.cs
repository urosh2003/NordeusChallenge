using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Game Data/Character Data", order = 1)]
public class CharacterSO : ScriptableObject
{
    public string characterId;
    public Sprite characterSprite;
}
