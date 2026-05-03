using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scrollable in-game battle log. Attach to the CombatCanvas.
/// Clears itself each time it is enabled (i.e. at the start of every combat).
///
/// Scene setup
/// ───────────
///   BattleLog GameObject
///     ScrollRect component      ← wire to scrollRect
///     Image (background)
///     Viewport  (child)
///       Image + Mask component
///       Content  (child)
///         ContentSizeFitter     → Vertical Fit: Preferred Size
///         TextMeshProUGUI       ← wire to logText
///           Text Wrapping: ON
///           Overflow: Overflow
/// </summary>
public class BattleLog : MonoBehaviour
{
    [SerializeField] ScrollRect      scrollRect;
    [SerializeField] TextMeshProUGUI logText;
    [SerializeField] int             maxLines = 150;

    private readonly List<string> _lines = new();

    void OnEnable()
    {
        _lines.Clear();
        if (logText) logText.text = string.Empty;
        CombatEventProcessor.OnAnyEvent += Append;
    }

    void OnDisable() => CombatEventProcessor.OnAnyEvent -= Append;

    private void Append(CombatEvent e)
    {
        if (e.EventType == CombatEventType.UNKNOWN) return;

        string color = EventColor(e.EventType);
        string text  = CombatEventFormatter.Format(e);
        _lines.Add($"<color={color}>{text}</color>");

        if (_lines.Count > maxLines)
            _lines.RemoveAt(0);

        logText.text = string.Join("\n", _lines);
        StartCoroutine(ScrollToBottom());
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private static string EventColor(CombatEventType type) => type switch
    {
        CombatEventType.DAMAGE_DEALT          => "#ff6b6b",
        CombatEventType.HEAL_RECEIVED         => "#6bff8b",
        CombatEventType.RESOURCE_SPENT        => "#aaaaaa",
        CombatEventType.RESOURCE_REGEN        => "#88ccff",
        CombatEventType.MOVE_USED             => "#ffffff",
        CombatEventType.STATUS_EFFECT_APPLIED => "#ffff88",
        CombatEventType.TURN_CHANGED          => "#88ccff",
        CombatEventType.ENVIRONMENT_EFFECT    => "#88ddff",
        CombatEventType.XP_GAINED             => "#ffd700",
        CombatEventType.GOLD_GAINED           => "#ffd700",
        CombatEventType.ITEM_DROPPED          => "#ffa500",
        CombatEventType.MOVE_LEARNT           => "#da70d6",
        CombatEventType.LEVEL_UP              => "#da70d6",
        CombatEventType.COMBAT_ENDED          => "#ffffff",
        _                                     => "#cccccc",
    };
}
