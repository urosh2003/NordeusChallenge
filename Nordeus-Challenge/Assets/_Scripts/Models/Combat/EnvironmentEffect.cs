using System;
using Newtonsoft.Json;

[Serializable]
public class EnvironmentEffect
{
    [JsonProperty("resourceType")] public string resourceType; // "HEALTH" | "MANA" | "STAMINA"
    [JsonProperty("effectType")]   public string effectType;   // "GAIN" | "LOSE"
    [JsonProperty("amount")]       public int    amount;
    [JsonProperty("isPercent")]    public bool   isPercent;
}
