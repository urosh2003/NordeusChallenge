using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// - Sets the environment name label from the server-side EnvironmentDefinition.
/// - Enables the matching tilemap root  and disables all others.
/// - Shows a tooltip on hover describing stat and resource effects.
///
/// </summary>
public class EnvironmentDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] TextMeshProUGUI environmentLabel;

    void OnEnable()
    {
        GameManager.OnCombatUpdated += HandleCombatUpdated;
        Refresh();
    }

    void OnDisable() => GameManager.OnCombatUpdated -= HandleCombatUpdated;

    private void HandleCombatUpdated(CombatResponse r)
    {
        if (r.events == null || r.events.Count == 0)
            Refresh();
    }

    private void Refresh()
    {
        var env = GameManager.Instance?.CurrentEnvironment;

        if (environmentLabel)
            environmentLabel.text = env?.name ?? string.Empty;

        ActivateTilemap(env?.id);
    }

    private void ActivateTilemap(string environmentId)
    {
        EnvironmentManager.Instance?.SetEnvironment(environmentId);
    }

    // ── Tooltip ───────────────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData _)
    {
        var env = GameManager.Instance?.CurrentEnvironment;
        if (env == null) return;

        Tooltip.Instance?.Show(
            env.name,
            BuildSubtitle(env),
            BuildBody(env),
            Input.mousePosition);
    }

    public void OnPointerExit(PointerEventData _) => Tooltip.Instance?.Hide();

    private static string BuildSubtitle(EnvironmentDefinition env)
    {
        bool hasStats    = env.statEffects != null &&
                           (env.statEffects.health  != 0 || env.statEffects.attack != 0 ||
                            env.statEffects.defense != 0 || env.statEffects.magic  != 0);
        bool hasResource = env.resourceEffects != null && env.resourceEffects.Count > 0;

        if (!hasStats && !hasResource) return "No special effects.";
        if (hasStats && hasResource)   return "Stat modifiers + per-round effects";
        if (hasStats)                  return "Stat modifiers";
        return "Per-round resource effects";
    }

    private static string BuildBody(EnvironmentDefinition env)
    {
        var sb = new StringBuilder();

        var s = env.statEffects;
        if (s != null)
        {
            if (s.health  != 0) AppendStat(sb, "Health",      s.health);
            if (s.attack  != 0) AppendStat(sb, "Attack",      s.attack);
            if (s.defense != 0) AppendStat(sb, "Defense",     s.defense);
            if (s.magic   != 0) AppendStat(sb, "Magic",       s.magic);
        }

        if (env.resourceEffects != null)
        {
            foreach (var e in env.resourceEffects)
            {
                string res    = Capitalize(e.resourceType.ToLower());
                string verb   = e.effectType == "GAIN" ? "Gain" : "Lose";
                string amount = e.isPercent ? $"{e.amount}%" : e.amount.ToString();
                if (sb.Length > 0) sb.Append('\n');
                sb.Append($"Each round: {verb} {amount} {res}");
            }
        }

        return sb.Length > 0 ? sb.ToString() : "No special effects.";
    }

    private static void AppendStat(StringBuilder sb, string label, int value)
    {
        if (sb.Length > 0) sb.Append('\n');
        string sign = value >= 0 ? "+" : string.Empty;
        sb.Append($"{label}: {sign}{value}");
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
}
