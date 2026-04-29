using System.Text;

public static class MoveTooltipContent
{
    public static string Cost(MoveConfig def)
    {
        if (def?.cost == null || def.cost.costType == "none") return "Free";
        return $"{def.cost.costValue} {Capitalize(def.cost.costType)}";
    }

    public static string Effects(MoveConfig def)
    {
        if (def == null) return string.Empty;
        var sb = new StringBuilder();

        if (def.mainEffects != null)
        {
            foreach (var e in def.mainEffects)
            {
                sb.Append($"{Capitalize(e.type)} {e.resource} {e.baseValue}");
                if (e.scaling != null)
                    sb.Append($" + {e.scaling.multiplier:0.##}× {e.scaling.stat}");
                if (!string.IsNullOrEmpty(e.reducedBy))
                    sb.Append($" (−{e.reducedBy})");
                sb.AppendLine($" → {e.target}");
            }
        }

        if (def.statusEffects != null)
        {
            foreach (var s in def.statusEffects)
                sb.AppendLine($"{(s.value >= 0 ? "+" : "")}{s.value} {s.type} for {s.duration}t → {s.target}");
        }

        if (!string.IsNullOrEmpty(def.description))
            sb.Append(def.description);

        return sb.ToString().TrimEnd();
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
}
