using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MovesPanel : MonoBehaviour
{
    public static MovesPanel Instance { get; private set; }

    [Header("Equipped Slots (assign 4 in order 0–3)")]
    public MoveSlotUI[] moveSlots;

    [Header("Known Moves Grid")]
    [SerializeField] Transform  knownMovesContainer;
    [SerializeField] GameObject moveItemPrefab;

    [Header("Buttons")]
    [SerializeField] Button confirmButton;
    [SerializeField] Button cancelButton;

    private string[]     _pendingEquipped;   // length 4, null = empty slot
    private List<string> _pendingKnown;

    private readonly List<GameObject> _spawnedItems = new();

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

        _pendingEquipped = new string[4];
        for (int i = 0; i < 4 && i < ps.equippedMoves.Count; i++)
            _pendingEquipped[i] = ps.equippedMoves[i];

        _pendingKnown = ps.knownMoves
            .Where(id => !_pendingEquipped.Contains(id))
            .ToList();

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

    // ── Button callbacks ──────────────────────────────────────────────────────

    public void OnConfirm()
    {
        if (!IsEditable) return;
        if (_pendingEquipped.Any(id => string.IsNullOrEmpty(id)))
        {
            Debug.LogWarning("[MovesPanel] All 4 slots must be filled.");
            return;
        }
        GameManager.Instance.EquipMoves(new List<string>(_pendingEquipped),
            ps =>
            {
                _pendingEquipped = new string[4];
                for (int i = 0; i < 4 && i < ps.equippedMoves.Count; i++)
                    _pendingEquipped[i] = ps.equippedMoves[i];
                _pendingKnown = ps.knownMoves
                    .Where(id => !System.Array.Exists(_pendingEquipped, e => e == id))
                    .ToList();
                Refresh();
            },
            err => Debug.LogError($"[MovesPanel] EquipMoves failed: {err}"));
    }

    public void OnCancel() => Close();

    // ── Pending-state mutations ───────────────────────────────────────────────

    public void TryMoveToSlot(string moveId, int slotIndex)
    {
        if (string.IsNullOrEmpty(moveId) || slotIndex < 0 || slotIndex >= 4) return;
        RemoveFromPendingState(moveId);

        var displaced = _pendingEquipped[slotIndex];
        if (!string.IsNullOrEmpty(displaced)) _pendingKnown.Add(displaced);

        _pendingEquipped[slotIndex] = moveId;
        Refresh();
    }

    public void TryMoveToKnown(string moveId)
    {
        if (string.IsNullOrEmpty(moveId)) return;
        RemoveFromPendingState(moveId);
        if (!_pendingKnown.Contains(moveId))
            _pendingKnown.Add(moveId);
        Refresh();
    }

    // ── Rebuild UI ────────────────────────────────────────────────────────────

    public void Refresh()
    {
        RefreshSlots();
        RefreshKnownGrid();
        UpdateConfirmButton();
    }

    private void RefreshSlots()
    {
        var cfg = GameManager.Instance.CurrentRunConfig;
        for (int i = 0; i < moveSlots.Length; i++)
        {
            string id  = i < _pendingEquipped.Length ? _pendingEquipped[i] : null;
            moveSlots[i]?.Refresh(id, cfg?.GetMove(id));
        }
    }

    private void RefreshKnownGrid()
    {
        foreach (var go in _spawnedItems) Destroy(go);
        _spawnedItems.Clear();

        var cfg = GameManager.Instance.CurrentRunConfig;
        foreach (var id in _pendingKnown)
        {
            var go = Instantiate(moveItemPrefab, knownMovesContainer);
            go.GetComponent<MoveItemUI>()?.Setup(id, cfg?.GetMove(id));
            _spawnedItems.Add(go);
        }
    }

    private void UpdateConfirmButton()
    {
        if (confirmButton)
            confirmButton.interactable = _pendingEquipped.All(id => !string.IsNullOrEmpty(id));
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void RemoveFromPendingState(string moveId)
    {
        _pendingKnown.Remove(moveId);
        for (int i = 0; i < _pendingEquipped.Length; i++)
        {
            if (_pendingEquipped[i] == moveId)
            {
                _pendingEquipped[i] = null;
                return;
            }
        }
    }
}
