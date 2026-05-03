using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays four stat rows and handles pending stat-point allocation.
///
/// Call Open() when the panel opens (resets any in-progress allocation).
/// Call UpdateEquipmentBonuses() when the pending equipment changes (preserves allocation).
///
/// Prefab requirements for statRowPrefab: StatRowUI with label, valueText,
/// allocatedText, plusButton, minusButton.
///
/// Scene setup
/// ───────────
///   StatsPanel (this script)
///     Container         — Layout group, receives StatRowUI prefabs
///     PendingLabel      TextMeshProUGUI — "X points to spend"
///     ConfirmStatsButton Button → OnConfirmStats()  (hidden when no pending points)
/// </summary>
public class StatsPanel : MonoBehaviour
{
    [SerializeField] Transform           container;
    [SerializeField] GameObject          statRowPrefab;
    [SerializeField] Button              confirmStatsButton;
    [SerializeField] TextMeshProUGUI     pendingPointsLabel;
    [SerializeField] TextMeshProUGUI     levelLabel;
    [SerializeField] TextMeshProUGUI     xpLabel;

    private CharacterStats   _baseStats;
    private readonly int[]   _equipBonus  = new int[4]; // health, attack, defense, magic
    private readonly int[]   _allocated   = new int[4];
    private int              _serverPendingPoints;

    private bool _enableChanges;

    private readonly List<GameObject> _rows = new();

    private static readonly string[] StatNames  = { "Health", "Attack", "Defense", "Magic" };

    private int AllocatedTotal    => _allocated[0] + _allocated[1] + _allocated[2] + _allocated[3];
    private int PointsRemaining   => _serverPendingPoints - AllocatedTotal;
    private bool HasPendingPoints => _serverPendingPoints > 0;

    // ── Public API ────────────────────────────────────────────────────────────

    /// Open standalone — reads current authoritative state directly from GameManager.
    /// Wire a HUD button to this.
    public void OpenStandalone()
    {
        var ps     = GameManager.Instance.PlayerState;
        var config = GameManager.Instance.CurrentRunConfig;
        if (ps == null) return;
        gameObject.SetActive(true);
        Open(ps.stats, ps.equipment.Clone(), config, ps.pendingStatPoints, true);
    }

    public void Close() => gameObject.SetActive(false);

    /// Full reset — call when the player info panel opens.
    public void Open(CharacterStats baseStats, Equipment pendingEquip, RunConfig config, int pendingStatPoints, bool enableChanges=false)
    {
        _enableChanges       = enableChanges;
        _baseStats           = baseStats;
        _serverPendingPoints = pendingStatPoints;
        System.Array.Clear(_allocated, 0, 4);
        ComputeEquipBonus(pendingEquip, config);
        RebuildRows(_enableChanges);
        RefreshConfirmButton();
        UpdatePendingLabel();
        UpdatePlayerLabels();
    }

    /// Equipment changed — update bonus column without resetting allocation.
    public void UpdateEquipmentBonuses(Equipment pendingEquip, RunConfig config)
    {
        ComputeEquipBonus(pendingEquip, config);
        RefreshAllRows();
    }

    // ── Button callback (wire to ConfirmStatsButton.OnClick) ─────────────────

    public void OnConfirmStats()
    {
        if (AllocatedTotal <= 0) return;
        GameManager.Instance.SpendLevelUpPoints(
            _allocated[0], _allocated[1], _allocated[2], _allocated[3],
            ps =>
            {
                _serverPendingPoints = ps.pendingStatPoints;
                _baseStats           = ps.stats;
                System.Array.Clear(_allocated, 0, 4);
                RebuildRows(_enableChanges);
                RefreshConfirmButton();
                UpdatePendingLabel();
                UpdatePlayerLabels();
            },
            err => Debug.LogError($"[StatsPanel] SpendLevelUpPoints failed: {err}"));
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void OnPlus(int idx)
    {
        if (PointsRemaining <= 0) return;
        _allocated[idx]++;
        RefreshAllRows();
        RefreshConfirmButton();
        UpdatePendingLabel();
    }

    private void OnMinus(int idx)
    {
        if (_allocated[idx] <= 0) return;
        _allocated[idx]--;
        RefreshAllRows();
        RefreshConfirmButton();
        UpdatePendingLabel();
    }

    private void RebuildRows(bool enableChanges)
    {
        foreach (var r in _rows) Destroy(r);
        _rows.Clear();

        int[] baseValues = { _baseStats.health, _baseStats.attack, _baseStats.defense, _baseStats.magic };

        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            var go  = Instantiate(statRowPrefab, container);
            var row = go.GetComponent<StatRowUI>();
            row?.Setup(StatNames[i], baseValues[i], _equipBonus[i], _allocated[i],
                canAdd:        HasPendingPoints && PointsRemaining > 0,
                canRemove:     _allocated[i] > 0,
                onPlus:        () => OnPlus(idx),
                onMinus:       () => OnMinus(idx),
                disableChange: !enableChanges);
            _rows.Add(go);
        }
    }

    private void RefreshAllRows()
    {
        int[] baseValues = { _baseStats.health, _baseStats.attack, _baseStats.defense, _baseStats.magic };

        for (int i = 0; i < _rows.Count && i < 4; i++)
        {
            var row = _rows[i].GetComponent<StatRowUI>();
            row?.UpdateDisplay(_allocated[i], _equipBonus[i],
                canAdd:    HasPendingPoints && PointsRemaining > 0,
                canRemove: _allocated[i] > 0);
        }
    }

    private void ComputeEquipBonus(Equipment eq, RunConfig config)
    {
        System.Array.Clear(_equipBonus, 0, 4);
        foreach (var id in eq.GetAllEquipped())
        {
            var bonus = config?.GetItem(id)?.bonusStats;
            if (bonus == null) continue;
            _equipBonus[0] += bonus.health;
            _equipBonus[1] += bonus.attack;
            _equipBonus[2] += bonus.defense;
            _equipBonus[3] += bonus.magic;
        }
    }

    private void RefreshConfirmButton()
    {
        if (!confirmStatsButton) return;
        confirmStatsButton.gameObject.SetActive(HasPendingPoints);
        confirmStatsButton.interactable = AllocatedTotal > 0;
    }

    private void UpdatePendingLabel()
    {
        if (!pendingPointsLabel) return;
        if (!HasPendingPoints)
        {
            pendingPointsLabel.text = string.Empty;
            return;
        }
        int remaining = PointsRemaining;
        pendingPointsLabel.text = remaining > 0
            ? $"{remaining} point{(remaining == 1 ? "" : "s")} remaining"
            : "Ready to confirm";
    }

    private void UpdatePlayerLabels()
    {
        var ps = GameManager.Instance?.PlayerState;
        if (levelLabel) levelLabel.text = ps != null ? $"Level {ps.level}" : string.Empty;
        if (xpLabel)    xpLabel.text    = ps != null ? $"XP: {ps.currentXp} / {ps.xpToNextLevel}" : string.Empty;
    }
}
