using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class EnvironmentDefinition
{
    [JsonProperty("id")]              public string                  id;
    [JsonProperty("name")]            public string                  name;
    [JsonProperty("resourceEffects")] public List<EnvironmentEffect> resourceEffects;
    [JsonProperty("statEffects")]     public StatEffects             statEffects;

    [Serializable]
    public class StatEffects
    {
        [JsonProperty("health")]  public int health;
        [JsonProperty("attack")]  public int attack;
        [JsonProperty("defense")] public int defense;
        [JsonProperty("magic")]   public int magic;
    }
}
