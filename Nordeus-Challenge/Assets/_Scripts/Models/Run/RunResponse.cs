using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class RunResponse
{
    public string runId;
    public string status;
    public string currentNodeId;
    public List<RunNode> nodes;
    public string activeCombatId; // non-empty when a combat is in progress for this run

    public RunStatus Status =>
        Enum.TryParse(status, out RunStatus s) ? s : RunStatus.ACTIVE;

    public bool IsCompleted => Status == RunStatus.COMPLETED;

    public RunNode CurrentNode => GetNode(currentNodeId);

    public RunNode GetNode(string nodeId) => nodes?.Find(n => n.id == nodeId);

    // Rows ordered bottom→top, nodes within each row ordered left→right by column.
    // Index 0 of the returned list = row 0 (start node).
    public List<List<RunNode>> GetRows()
    {
        if (nodes == null) return new List<List<RunNode>>();
        return nodes
            .GroupBy(n => n.row)
            .OrderBy(g => g.Key)
            .Select(g => g.OrderBy(n => n.column).ToList())
            .ToList();
    }
}
