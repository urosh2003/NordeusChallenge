using UnityEngine;

public class FloatingTextManager : MonoBehaviour
{
    [SerializeField] private RectTransform playerNumbers;
    [SerializeField] private RectTransform enemyNumbers;
    [SerializeField] private GameObject floatingTextPrefab;

    void Start()
    {
        CombatEventProcessor.OnAnyEvent += ProcessEvent;
    }
    void OnDestroy()
    {
        CombatEventProcessor.OnAnyEvent -= ProcessEvent;
    }

    private void ProcessEvent(CombatEvent e)
    {
        if (e.type == nameof(CombatEventType.DAMAGE_DEALT))
        {
            SpawnNumber(e.targetId, e.amount, ResourceType.HEALTH, false);
        }
        else if (e.type == nameof(CombatEventType.HEAL_RECEIVED))
        {
            SpawnNumber(e.targetId, e.amount, ResourceType.HEALTH, true);
        }
        else if (e.type == nameof(CombatEventType.RESOURCE_REGEN))
        {
            SpawnNumber(e.targetId, e.staminaGained, ResourceType.STAMINA, true);
            SpawnNumber(e.targetId, e.manaGained, ResourceType.MANA, true);
        }
        else if (e.type == nameof(CombatEventType.RESOURCE_SPENT))
        {
            if(e.costType=="mana")
                SpawnNumber(e.actorId, e.amount, ResourceType.MANA, false);
            else if(e.costType=="stamina")
                SpawnNumber(e.actorId, e.amount, ResourceType.STAMINA, false);
            else if(e.costType=="health")
                SpawnNumber(e.actorId, e.amount, ResourceType.HEALTH, false);
        }
        else if (e.type == nameof(CombatEventType.ENVIRONMENT_EFFECT))
        {
            switch (e.resourceType)
            {
                case "HEALTH":
                    SpawnNumber(e.targetId, e.amount, ResourceType.HEALTH, e.amount>0);
                    break;
                case "MANA":
                    SpawnNumber(e.targetId, e.amount, ResourceType.MANA, e.amount>0);
                    break;
                case "STAMINA":
                    SpawnNumber(e.targetId, e.amount, ResourceType.STAMINA, e.amount>0);
                    break;
            }
        }
        else if (e.type == nameof(CombatEventType.XP_GAINED))
        {
            SpawnNumber(e.targetId, e.amount, ResourceType.MANA, true);
        }
    }
    
    private void SpawnNumber(string targetId, int amount, ResourceType resourceType, bool isGain)
    {
        bool isPlayerTarget = (targetId == "player");
        RectTransform parent = isPlayerTarget ? playerNumbers : enemyNumbers;
        
        GameObject go = Instantiate(floatingTextPrefab, parent);
        go.GetComponent<FloatingText>().Initialize(amount, resourceType, isGain);
    }
    
    
}
