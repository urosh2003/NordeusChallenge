using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Game Data/Move Data", order = 2)]
public class MoveSO : ScriptableObject
{
    public string moveId;
    public Sprite moveIcon;
    public AudioClip moveSound;
}
