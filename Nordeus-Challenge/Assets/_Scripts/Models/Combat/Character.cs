using System;
using System.Collections.Generic;

[Serializable]
public class Character
{
    public string id;
    public string name;
    public int currentHp;
    public int maxHp;
    public int currentMana;
    public int maxMana;
    public int manaPerTurn;
    public int currentStamina;
    public int maxStamina;
    public int staminaPerTurn;
    public CharacterStats stats;
    public List<string> moves;   // equipped move IDs for this combat
}

[Serializable]
public class CharacterStats
{
    public int health;
    public int attack;
    public int defense;
    public int magic;
}
