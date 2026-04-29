using System.Collections.Generic;
using Newtonsoft.Json;

// Deserialized with Newtonsoft.Json (not JsonUtility) because moves/items
// are JSON objects keyed by ID, which JsonUtility cannot handle.

public class RunConfig
{
    [JsonProperty("runId")]        public string runId;
    [JsonProperty("encounters")]   public List<RunConfigEncounter> encounters;
    [JsonProperty("moves")]        public Dictionary<string, MoveConfig> moves;
    [JsonProperty("items")]        public Dictionary<string, ItemDefinition> items;
    [JsonProperty("player")]       public RunConfigPlayer player;
    [JsonProperty("environments")] public Dictionary<string, EnvironmentDefinition> environments;

    // ── Lookup helpers ────────────────────────────────────────────────────────

    public MoveConfig GetMove(string id) =>
        id != null && moves != null && moves.TryGetValue(id, out var m) ? m : null;

    public ItemDefinition GetItem(string id) =>
        id != null && items != null && items.TryGetValue(id, out var i) ? i : null;

    public RunConfigEncounter GetEncounter(int index) =>
        encounters?.Find(e => e.index == index);

    public EnvironmentDefinition GetEnvironment(string id) =>
        id != null && environments != null && environments.TryGetValue(id, out var e) ? e : null;
}
