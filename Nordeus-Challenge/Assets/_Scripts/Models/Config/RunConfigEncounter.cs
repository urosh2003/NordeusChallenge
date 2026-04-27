using System;
using System.Collections.Generic;

[Serializable]
public class RunConfigEncounter
{
    public int index;
    public string enemyDefinitionId;
    public string enemyName;
    public int enemyLevel;
    public CharacterStats enemyStats;
    public int enemyMaxHp;
    public int enemyMaxMana;
    public int enemyManaPerTurn;
    public int enemyMaxStamina;
    public int enemyStaminaPerTurn;
    public List<string> enemyMoves;
    public List<string> possibleDrops;
    public int completions;

    public bool IsCleared => completions > 0;
}
