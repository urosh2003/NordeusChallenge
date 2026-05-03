using UnityEngine;
using UnityEngine.UI;

public class LevelUpAvailable : MonoBehaviour
{    
    [SerializeField] Image icon;
    
    void Start()
    {
        var ps = GameManager.Instance.PlayerState;
        if (ps == null || !ps.HasPendingLevelUp)
        {
            icon.enabled = false;
        }
        else
        {   
            icon.enabled = true;
        }
        
        CombatEventProcessor.OnLevelUp += HandleLevelUp;
        GameManager.OnPlayerStateUpdated += Reset;
    }

    void OnDestroy()
    {
        CombatEventProcessor.OnLevelUp -= HandleLevelUp;
        GameManager.OnPlayerStateUpdated -= Reset;
    }

    private void HandleLevelUp(CombatEvent combatEvent) => icon.enabled = true;

    public void Reset(PlayerStateResponse playerStateResponse)
    {
        icon.enabled = playerStateResponse.HasPendingLevelUp;
    }
}
