using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Rebuilds the run map whenever the run state changes.
/// Attach this to the MapCanvas panel (the overlay).
///
/// Scene setup:
///   - container    : a RectTransform that fills the panel — nodes are placed inside it
///   - nodePrefab   : Button + RunNodeButton + Image (background) + level TextMeshPro
///   - edgePrefab   : an Image (white, 1px tall) — stretched + rotated into a line
///
/// Layout:
///   Rows go bottom (row 0) to top (row 9).
///   Nodes within a row are spread evenly across the container width.
///   Edges connect each node's centre to the centres of its nextNodeIds.
///
/// Click logic (delegated to RunNodeButton):
///   CURRENT node    → GameManager.StartNodeCombat(nodeId)
///   NEXT_CHOICE node → GameManager.ContinueRun(nodeId)   [only after current is cleared]
/// </summary>
public class RunMapUI : MonoBehaviour
{
    [Header("References")]
    public RectTransform container;
    public GameObject    nodePrefab;
    public GameObject    edgePrefab;

    [Header("Layout")]
    public float paddingX    = 80f;
    public float paddingY    = 60f;
    public float nodeSize    = 64f;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private readonly List<GameObject>              _spawned   = new();
    private readonly Dictionary<string, Vector2>   _positions = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void OnEnable()
    {
        GameManager.OnRunUpdated += Rebuild;
        GameManager.Instance?.GetRun(onError: _ => { });
    }

    void OnDisable()
    {
        GameManager.OnRunUpdated -= Rebuild;
    }

    // ── Map building ──────────────────────────────────────────────────────────

    private void Rebuild(RunResponse run)
    {
        // container.rect is (0,0) until the Canvas has done its first layout pass.
        // Defer one frame so positions are calculated against real dimensions.
        if (container.rect.width < 1f)
        {
            StartCoroutine(RebuildNextFrame(run));
            return;
        }
        DoBuild(run);
    }

    private IEnumerator RebuildNextFrame(RunResponse run)
    {
        yield return null;
        DoBuild(run);
    }

    private void DoBuild(RunResponse run)
    {
        Clear();
        
        if (run?.nodes == null) return;

        var rows = run.GetRows();
        int totalRows = rows.Count;
        if (totalRows == 0) return;

        Rect bounds      = container.rect;
        float usableW    = bounds.width  - paddingX * 2;
        float usableH    = bounds.height - paddingY * 2;

        // ── Place nodes ───────────────────────────────────────────────────────

        foreach (var row in rows)
        {
            int count = row.Count;
            foreach (var node in row)
            {
                float x = paddingX + (node.column + 0.5f) / count * usableW;
                float y = paddingY + (float)node.row / Mathf.Max(1, totalRows - 1) * usableH;
                var pos = new Vector2(x, y);
                _positions[node.id] = pos;

                var go = Instantiate(nodePrefab, container);
                ConfigureRect(go, pos, new Vector2(nodeSize, nodeSize));
                go.GetComponent<RunNodeButton>()?.Setup(node, run, id => OnNodeClicked(id, run));
                _spawned.Add(go);
            }
        }

        // ── Draw edges (behind nodes) ─────────────────────────────────────────

        foreach (var node in run.nodes)
        {
            if (node.nextNodeIds == null) continue;
            if (!_positions.TryGetValue(node.id, out var from)) continue;

            foreach (var nextId in node.nextNodeIds)
            {
                if (!_positions.TryGetValue(nextId, out var to)) continue;
                SpawnEdge(from, to, node);
            }
        }
    }

    private void SpawnEdge(Vector2 from, Vector2 to, RunNode fromNode)
    {
        var go = Instantiate(edgePrefab, container);
        go.transform.SetAsFirstSibling(); // render under nodes

        var dir    = to - from;
        var length = dir.magnitude;
        var angle  = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = rt.anchorMax = Vector2.zero;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = (from + to) * 0.5f;
        rt.sizeDelta        = new Vector2(length, 3f);
        rt.localRotation    = Quaternion.Euler(0f, 0f, angle);

        // Dim edges that lead from cleared nodes (path already taken)
        var img = go.GetComponent<Image>();
        if (img != null)
            img.color = fromNode.IsCleared
                ? new Color(1f, 1f, 1f, 0.25f)
                : new Color(1f, 1f, 1f, 0.7f);

        _spawned.Add(go);
    }

    // ── Node click handling ───────────────────────────────────────────────────

    private void OnNodeClicked(RunNode node, RunResponse run)
    {
        var current = run.CurrentNode;
        //if (current == null) return;

        bool isCurrent = node.id == run.currentNodeId;
        bool isNext    = current.IsCleared &&
                         current.nextNodeIds != null &&
                         current.nextNodeIds.Contains(node.id);

        if (!isCurrent && !isNext) return;

        if (node.IsCombat)
            GameManager.Instance.StartNodeCombat(node.id);
        else if (node.Type == NodeType.REST_SITE)
            RestPanel.Instance.Open(node.id);
        else
            GameManager.Instance.EnterNode(node.id);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void Clear()
    {
        foreach (var go in _spawned) Destroy(go);
        _spawned.Clear();
        _positions.Clear();
    }

    private static void ConfigureRect(GameObject go, Vector2 pos, Vector2 size)
    {
        var rt           = go.GetComponent<RectTransform>();
        rt.anchorMin     = rt.anchorMax = Vector2.zero;
        rt.pivot         = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta     = size;
    }
}
