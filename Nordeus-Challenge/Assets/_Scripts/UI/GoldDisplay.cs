using TMPro;
using UnityEngine;

public class GoldDisplay : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI label;
    [SerializeField] string          format = "Gold: {0}";

    private int _gold;

    void OnEnable()
    {
        GameManager.OnPlayerStateUpdated          += OnPlayerStateUpdated;
        CombatEventProcessor.OnGoldGained         += OnGoldGained;
        CombatEventProcessor.OnAllEventsProcessed += OnAllEventsProcessed;

        var ps = GameManager.Instance?.PlayerState;
        if (ps != null) SetGold(ps.gold);
    }

    void OnDisable()
    {
        GameManager.OnPlayerStateUpdated          -= OnPlayerStateUpdated;
        CombatEventProcessor.OnGoldGained         -= OnGoldGained;
        CombatEventProcessor.OnAllEventsProcessed -= OnAllEventsProcessed;
    }

    private void OnPlayerStateUpdated(PlayerStateResponse ps)
    {
        // While the event queue is draining, gold updates come through OnGoldGained instead.
        if (CombatEventProcessor.Instance != null && CombatEventProcessor.Instance.IsProcessing) return;
        SetGold(ps.gold);
    }

    private void OnGoldGained(CombatEvent e) => SetGold(_gold + e.amount);

    private void OnAllEventsProcessed(CombatState _, PlayerStateResponse player)
    {
        if (player != null) SetGold(player.gold);
    }

    private void SetGold(int gold)
    {
        _gold = gold;
        if (label) label.text = string.Format(format, gold);
    }
}
