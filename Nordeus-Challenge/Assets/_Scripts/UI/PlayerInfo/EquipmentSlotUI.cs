using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One equipment slot in the player-info panel.
/// Acts as both a drag SOURCE (drag the equipped item out) and a DROP TARGET.
///
/// Prefab requirements:
///   - CanvasGroup    (for blocking raycasts during drag)
///   - Image          icon          (the item's sprite; hidden when empty)
///   - GameObject     emptyOverlay  (shown when no item is equipped)
///   - TextMeshProUGUI label        (optional: slot name, e.g. "Main Hand")
/// </summary>
public class EquipmentSlotUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] Image           icon;
    [SerializeField] GameObject      emptyOverlay;
    [SerializeField] CanvasGroup     canvasGroup;
    [SerializeField] TextMeshProUGUI slotLabel;

    private EquipmentSlot  _slot;
    private string         _itemId;
    private ItemDefinition _itemDef;

    // ── Setup ─────────────────────────────────────────────────────────────────

    public void Refresh(EquipmentSlot slot, string itemId, ItemDefinition def)
    {
        _slot    = slot;
        _itemId  = itemId;
        _itemDef = def;

        if (slotLabel) slotLabel.text = SlotDisplayName(slot);

        bool filled = !string.IsNullOrEmpty(itemId);
        if (emptyOverlay) emptyOverlay.SetActive(!filled);
        if (icon)
        {
            icon.enabled = filled;
            // Assign sprite here once item sprites are wired into SpriteManager:
            icon.sprite = SpriteManager.Instance.GetItemIcon(itemId);
        }
    }

    // ── Drag source (drag equipped item out) ──────────────────────────────────

    public void OnBeginDrag(PointerEventData e)
    {
        if (string.IsNullOrEmpty(_itemId)) return;
        if (!PlayerInfoPanel.Instance.IsEditable) return;

        if (canvasGroup) canvasGroup.blocksRaycasts = false;
        DragDropManager.Instance.BeginDrag(_itemId, icon?.sprite, e.position);
    }

    public void OnDrag(PointerEventData e) => DragDropManager.Instance.UpdateGhost(e.position);

    public void OnEndDrag(PointerEventData _)
    {
        if (canvasGroup) canvasGroup.blocksRaycasts = true;
        DragDropManager.Instance.EndDrag();
        if (!DragDropManager.Instance.WasDropHandled)
            PlayerInfoPanel.Instance.Refresh();
    }

    // ── Drop target ───────────────────────────────────────────────────────────

    public void OnDrop(PointerEventData _)
    {
        if (!DragDropManager.Instance.IsDragging) return;
        string dragged = DragDropManager.Instance.DraggedItemId;
        DragDropManager.Instance.NotifyDropHandled();
        DragDropManager.Instance.EndDrag();  // hide ghost before Refresh destroys the source item
        PlayerInfoPanel.Instance.TryMoveToSlot(dragged, _slot);
    }

    // ── Tooltip ───────────────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData e)
    {
        if (_itemDef != null)
            Tooltip.Instance?.Show(_itemDef.name, _itemDef.itemType, BuildDescription(_itemDef), e.position);
    }

    public void OnPointerExit(PointerEventData _) => Tooltip.Instance?.Hide();

    private static string BuildDescription(ItemDefinition def)
    {
        var sb = new StringBuilder();
        if (def.bonusStats != null)
        {
            if (def.bonusStats.health  != 0) sb.AppendLine($"Health:  {Signed(def.bonusStats.health)}");
            if (def.bonusStats.attack  != 0) sb.AppendLine($"Attack:  {Signed(def.bonusStats.attack)}");
            if (def.bonusStats.defense != 0) sb.AppendLine($"Defense: {Signed(def.bonusStats.defense)}");
            if (def.bonusStats.magic   != 0) sb.AppendLine($"Magic:   {Signed(def.bonusStats.magic)}");
        }
        if (def.passiveEffects != null)
            foreach (var pe in def.passiveEffects)
                sb.AppendLine($"{pe.type}: +{pe.value}");
        return sb.ToString();
    }

    private static string Signed(int v) => v >= 0 ? $"+{v}" : $"{v}";

    private static string SlotDisplayName(EquipmentSlot s) => s switch
    {
        EquipmentSlot.MAIN_HAND => "Main Hand",
        EquipmentSlot.OFF_HAND  => "Off Hand",
        EquipmentSlot.ARMOR     => "Armor",
        EquipmentSlot.GLOVES    => "Gloves",
        EquipmentSlot.SHOES     => "Shoes",
        EquipmentSlot.AMULET    => "Amulet",
        EquipmentSlot.RING      => "Ring",
        _                       => s.ToString()
    };
}
