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

    private const int PointsPerLevel = 3;

    private CharacterStats   _baseStats;
    private readonly int[]   _equipBonus  = new int[4]; // health, attack, defense, magic
    private readonly int[]   _allocated   = new int[4];
    private int              _serverPendingPoints;

    private readonly List<GameObject> _rows = new();

    private static readonly string[] StatNames  = { "Health", "Attack", "Defense", "Magic" };

    private int AllocatedTotal      => _allocated[0] + _allocated[1] + _allocated[2] + _allocated[3];
    private int PointsLeftInBatch   => Mathf.Min(_serverPendingPoints, PointsPerLevel) - AllocatedTotal;
    private bool HasPendingLevel    => _serverPendingPoints >= PointsPerLevel;

    // ── Public API ────────────────────────────────────────────────────────────

    /// Full reset — call when the player info panel opens.
    public void Open(CharacterStats baseStats, Equipment pendingEquip, RunConfig config, int pendingStatPoints)
    {
        _baseStats           = baseStats;
        _serverPendingPoints = pendingStatPoints;
        System.Array.Clear(_allocated, 0, 4);
        ComputeEquipBonus(pendingEquip, config);
        RebuildRows();
        RefreshConfirmButton();
        UpdatePendingLabel();
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
        if (AllocatedTotal != PointsPerLevel) return;
        GameManager.Instance.SpendLevelUpPoints(
            _allocated[0], _allocated[1], _allocated[2], _allocated[3],
            ps =>
            {
                _serverPendingPoints = ps.pendingStatPoints;
                _baseStats           = ps.stats;
                System.Array.Clear(_allocated, 0, 4);
                RefreshAllRows();
                RefreshConfirmButton();
                UpdatePendingLabel();
            },
            err => Debug.LogError($"[StatsPanel] SpendLevelUpPoints failed: {err}"));
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void OnPlus(int idx)
    {
        if (PointsLeftInBatch <= 0) return;
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

    private void RebuildRows()
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
                canAdd:    HasPendingLevel && PointsLeftInBatch > 0,
                canRemove: _allocated[i] > 0,
                onPlus:    () => OnPlus(idx),
                onMinus:   () => OnMinus(idx));
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
                canAdd:    HasPendingLevel && PointsLeftInBatch > 0,
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
        confirmStatsButton.gameObject.SetActive(HasPendingLevel);
        confirmStatsButton.interactable = AllocatedTotal == PointsPerLevel;
    }

    private void UpdatePendingLabel()
    {
        if (!pendingPointsLabel) return;
        int left = PointsLeftInBatch;
        if (!HasPendingLevel)
        {
            pendingPointsLabel.text = string.Empty;
            return;
        }
        pendingPointsLabel.text = left > 0
            ? $"{left} point{(left == 1 ? "" : "s")} left to allocate"
            : "Ready to confirm";
    }
}
