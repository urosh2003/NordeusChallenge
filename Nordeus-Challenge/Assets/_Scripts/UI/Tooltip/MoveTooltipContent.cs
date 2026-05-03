using System.Text;

public static class MoveTooltipContent
{
    public static string Cost(MoveConfig def)
    {
        if (def?.cost == null || def.cost.costType == "none") return "Cost: Free";
        return $"Cost: {def.cost.costValue} {Cap(def.cost.costType)}";
    }

    public static string Effects(MoveConfig def)
    {
        if (def == null) return string.Empty;
        var sb = new StringBuilder();

        if (def.mainEffects != null)
        {
            foreach (var e in def.mainEffects)
            {
                sb.Append($"{Cap(e.type)} {Cap(e.resource)}:  {e.baseValue}");

                if (e.scaling != null)
                    sb.Append($" + {e.scaling.multiplier:0.##}× {Cap(e.scaling.stat)}");

                if (!string.IsNullOrEmpty(e.reducedBy))
                    sb.Append($"  (reduced by {Cap(e.reducedBy)})");

                sb.AppendLine($"\nTarget: {Cap(e.target)}");
            }
        }

        if (def.statusEffects != null)
        {
            foreach (var s in def.statusEffects)
            {
                string sign = s.value >= 0 ? "+" : "";
                sb.AppendLine($"{Cap(s.type)}: {sign}{s.value}  for {s.duration} turns  →  {Cap(s.target)}");
            }
        }

        if (!string.IsNullOrEmpty(def.description))
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.Append(def.description);
        }

        return sb.ToString().TrimEnd();
    }

    private static string Cap(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1).ToLower();
}
