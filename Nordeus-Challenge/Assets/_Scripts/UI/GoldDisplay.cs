using TMPro;
using UnityEngine;

public class GoldDisplay : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI label;
    [SerializeField] string          format = "Gold: {0}";

    void OnEnable()
    {
        GameManager.OnPlayerStateUpdated += Refresh;
        var ps = GameManager.Instance?.PlayerState;
        if (ps != null) Refresh(ps);
    }

    void OnDisable() => GameManager.OnPlayerStateUpdated -= Refresh;

    private void Refresh(PlayerStateResponse ps)
    {
        if (label) label.text = string.Format(format, ps.gold);
    }
}
