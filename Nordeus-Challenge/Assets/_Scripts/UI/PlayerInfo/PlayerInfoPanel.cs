using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main controller for the player info / equipment screen.
///
/// Open it from any map-screen button: PlayerInfoPanel.Instance.Open()
/// It shows read-only during active combat, fully interactive otherwise.
///
/// Scene setup
/// ───────────
///   Attach to a full-screen panel GameObject, initially inactive.
///   Wire all seven EquipmentSlotUI references in the Inspector.
///   Wire inventoryContainer (GridLayoutGroup), inventoryItemPrefab, statsPanel.
///   Wire confirmButton and cancelButton (hidden during combat).
///
/// Drag-drop flow
/// ──────────────
///   1. Player drags InventoryItemUI  → EquipmentSlotUI.OnDrop  → TryMoveToSlot
///   2. Player drags EquipmentSlotUI  → InventoryDropZone.OnDrop → TryMoveToInventory
///   3. Player drags EquipmentSlotUI  → another slot.OnDrop      → TryMoveToSlot (swap)
///   All changes are local until OnConfirm is called.
/// </summary>
public class PlayerInfoPanel : MonoBehaviour
{
    public static PlayerInfoPanel Instance { get; private set; }

    // ── Equipment slot references ─────────────────────────────────────────────
    [Header("Equipment Slots")]
    public EquipmentSlotUI mainHandSlot;
    public EquipmentSlotUI offHandSlot;
    public EquipmentSlotUI armorSlot;
    public EquipmentSlotUI glovesSlot;
    public EquipmentSlotUI shoesSlot;
    public EquipmentSlotUI amuletSlot;
    public EquipmentSlotUI ringSlot;

    [Header("Inventory")]
    [SerializeField] Transform  inventoryContainer;
    [SerializeField] GameObject inventoryItemPrefab;

    [Header("Stats")]
    [SerializeField] StatsPanel statsPanel;

    [Header("Buttons")]
    [SerializeField] Button confirmButton;
    [SerializeField] Button cancelButton;

    // ── Pending state (local copy until confirmed) ────────────────────────────
    private Equipment    _pendingEquip;
    private List<string> _pendingInventory;

    private readonly List<GameObject> _spawnedInventoryItems = new();

    // ── API ───────────────────────────────────────────────────────────────────

    /// True when outside of combat — drag-drop and Confirm are active.
    public bool IsEditable => !GameManager.Instance.IsCombatActive;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        gameObject.SetActive(false);
    }

    // ── Open / Close ──────────────────────────────────────────────────────────

    public void Open()
    {
        var ps = GameManager.Instance.PlayerState;
        if (ps == null) return;

        _pendingEquip     = ps.equipment.Clone();
        _pendingInventory = new List<string>(ps.inventory);

        gameObject.SetActive(true);

        bool editable = IsEditable;
        if (confirmButton) confirmButton.gameObject.SetActive(editable);
        if (cancelButton)  cancelButton.gameObject.SetActive(editable);

        Refresh();
    }

    public void Close()
    {
        Tooltip.Instance?.Hide();
        gameObject.SetActive(false);
    }

    // ── Button callbacks (wire to Inspector OnClick) ──────────────────────────

    public void OnConfirm()
    {
        if (!IsEditable) return;
        GameManager.Instance.SetEquipment(_pendingEquip,
            _ => Close(),
            err => Debug.LogError($"[PlayerInfoPanel] SetEquipment failed: {err}"));
    }

    public void OnCancel() => Close();

    // ── Pending-state mutations (called by drag-drop components) ──────────────

    /// Move itemId into targetSlot, displacing whatever was there to inventory.
    /// Validates type compatibility and two-handed/shield conflicts.
    public void TryMoveToSlot(string itemId, EquipmentSlot targetSlot)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        var config = GameManager.Instance.CurrentRunConfig;
        var def    = config?.GetItem(itemId);
        if (def == null || !Equipment.SlotAccepts(targetSlot, def.ItemType)) return;

        RemoveFromPendingState(itemId);

        // Displace current occupant
        var displaced = _pendingEquip.GetBySlot(targetSlot);
        if (!string.IsNullOrEmpty(displaced)) _pendingInventory.Add(displaced);

        // Two-handed weapon clears off-hand
        if (def.ItemType == ItemType.TWO_HANDED)
        {
            var oh = _pendingEquip.offHand;
            if (!string.IsNullOrEmpty(oh)) { _pendingInventory.Add(oh); _pendingEquip.offHand = null; }
        }

        // Shield clears two-handed from main-hand
        if (targetSlot == EquipmentSlot.OFF_HAND)
        {
            var mh = _pendingEquip.mainHand;
            if (!string.IsNullOrEmpty(mh) && config.GetItem(mh)?.ItemType == ItemType.TWO_HANDED)
            { _pendingInventory.Add(mh); _pendingEquip.mainHand = null; }
        }

        _pendingEquip.SetBySlot(targetSlot, itemId);
        Refresh();
    }

    /// Move itemId from whatever slot it occupies back to inventory.
    public void TryMoveToInventory(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        RemoveFromPendingState(itemId);
        if (!_pendingInventory.Contains(itemId))
            _pendingInventory.Add(itemId);
        Refresh();
    }

    // ── Rebuild UI ────────────────────────────────────────────────────────────

    public void Refresh()
    {
        RefreshEquipmentSlots();
        RefreshInventoryGrid();
        RefreshStats();
    }

    private void RefreshEquipmentSlots()
    {
        var cfg = GameManager.Instance.CurrentRunConfig;
        void R(EquipmentSlotUI ui, string id) =>
            ui?.Refresh(id, string.IsNullOrEmpty(id) ? null : cfg?.GetItem(id));

        R(mainHandSlot, _pendingEquip.mainHand);
        R(offHandSlot,  _pendingEquip.offHand);
        R(armorSlot,    _pendingEquip.armor);
        R(glovesSlot,   _pendingEquip.gloves);
        R(shoesSlot,    _pendingEquip.shoes);
        R(amuletSlot,   _pendingEquip.amulet);
        R(ringSlot,     _pendingEquip.ring);
    }

    private void RefreshInventoryGrid()
    {
        foreach (var go in _spawnedInventoryItems) Destroy(go);
        _spawnedInventoryItems.Clear();

        var cfg = GameManager.Instance.CurrentRunConfig;
        foreach (var id in _pendingInventory)
        {
            var go = Instantiate(inventoryItemPrefab, inventoryContainer);
            go.GetComponent<InventoryItemUI>()?.Setup(id, cfg?.GetItem(id));
            _spawnedInventoryItems.Add(go);
        }
    }

    private void RefreshStats()
    {
        var ps = GameManager.Instance.PlayerState;
        if (ps == null || statsPanel == null) return;
        statsPanel.Refresh(ps.stats, _pendingEquip, GameManager.Instance.CurrentRunConfig);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void RemoveFromPendingState(string itemId)
    {
        _pendingInventory.Remove(itemId);
        foreach (EquipmentSlot s in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            if (_pendingEquip.GetBySlot(s) == itemId)
            {
                _pendingEquip.SetBySlot(s, null);
                return;
            }
        }
    }
}
