using System;

[Serializable]
public class StatDistributionRequest
{
    public int health;
    public int attack;
    public int defense;
    public int magic;

    public StatDistributionRequest(int health, int attack, int defense, int magic)
    {
        this.health  = health;
        this.attack  = attack;
        this.defense = defense;
        this.magic   = magic;
    }

    // Must sum to at least 1, no negatives — server validates upper bound
    public bool IsValid => health + attack + defense + magic >= 1
                        && health >= 0 && attack >= 0 && defense >= 0 && magic >= 0;
}
