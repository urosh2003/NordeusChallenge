using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One playable class. Create one asset per class (knight, witch, ...).
/// classId must match the id in characters.json exactly.
/// </summary>
[CreateAssetMenu(fileName = "NewClass", menuName = "Game Data/Class Data", order = 4)]
public class ClassSO : ScriptableObject
{
    public string       classId;
    public string       className;
    [TextArea] public string description;
    public Sprite       portrait;

    [Header("Base Stats (mirrors characters.json)")]
    public int baseHealth;
    public int baseAttack;
    public int baseDefense;
    public int baseMagic;

    [Header("Starting Moves (display names only)")]
    public List<string> startingMoveNames;
}
