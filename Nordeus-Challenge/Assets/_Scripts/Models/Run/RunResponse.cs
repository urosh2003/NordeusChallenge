using System;
using System.Collections.Generic;

[Serializable]
public class RunResponse
{
    public string runId;
    public string status;              // matches RunStatus enum names
    public int currentEncounterIndex;
    public List<EncounterSlot> encounters;

    public RunStatus Status =>
        Enum.TryParse(status, out RunStatus s) ? s : RunStatus.ACTIVE;

    public bool IsCompleted => Status == RunStatus.COMPLETED;

    public EncounterSlot CurrentEncounter =>
        encounters != null && currentEncounterIndex < encounters.Count
            ? encounters[currentEncounterIndex]
            : null;
}
