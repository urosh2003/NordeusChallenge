using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Single status-effect icon cell.
///
/// Prefab requirements
/// ───────────────────
///   StatusEffectIcon (this script)
///     Icon        Image                ← wire to icon
///     Duration    TextMeshProUGUI      ← wire to durationText
/// </summary>
public class StatusEffectIconUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image              icon;
    [SerializeField] TextMeshProUGUI    durationText;

    public ActiveEffectInfo Effect { get; private set; }

    public void Setup(ActiveEffectInfo effect)
    {
        Effect = effect;
        if (icon)
        {
            var sprite   = SpriteManager.Instance?.GetStatTypeIcon(effect.effectType, effect.value);
            icon.sprite  = sprite;
            icon.enabled = sprite != null;
        }
        RefreshDuration();
    }

    public void SetDuration(int duration)
    {
        Effect.duration = duration;
        RefreshDuration();
    }

    public void UpdateEffect(int value, int duration)
    {
        Effect.value    = value;
        Effect.duration = duration;
        RefreshDuration();
    }

    private void RefreshDuration()
    {
        if (durationText) durationText.text = Effect.duration.ToString();
    }

    // ── Tooltip ───────────────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData _)
    {
        if (Effect == null) return;
        string turns = Effect.duration == 1 ? "1 turn" : $"{Effect.duration} turns";

        switch (Effect.effectType)
        {
            case "bleed":
                Tooltip.Instance?.Show("Bleed",
                    $"{Effect.duration} damage per turn",
                    $"Damage decreases by 1 each turn.",
                    Input.mousePosition);
                break;
            case "poison":
                Tooltip.Instance?.Show("Poison",
                    "10% max HP per turn",
                    $"Expires in {turns}.",
                    Input.mousePosition);
                break;
            default:
                string statName = char.ToUpper(Effect.effectType[0]) + Effect.effectType.Substring(1);
                string sign     = Effect.value >= 0 ? "+" : string.Empty;
                string kind     = Effect.value >= 0 ? "Buff" : "Debuff";
                Tooltip.Instance?.Show(
                    $"{statName} {kind}",
                    $"{sign}{Effect.value} {statName}",
                    $"Expires in {turns}.",
                    Input.mousePosition);
                break;
        }
    }

    public void OnPointerExit(PointerEventData _) => Tooltip.Instance?.Hide();
}
