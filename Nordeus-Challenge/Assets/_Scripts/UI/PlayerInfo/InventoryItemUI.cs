using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One item tile in the inventory grid.
/// Drag it onto an EquipmentSlotUI to equip it, or drop it on InventoryDropZone
/// (or another item) to return an equipped item to inventory.
///
/// Prefab requirements:
///   - CanvasGroup       (for blocking raycasts during drag)
///   - TextMeshProUGUI   nameLabel   (item name)
///   - Image             icon        (optional; wire when item sprites are added)
/// </summary>
public class InventoryItemUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] TextMeshProUGUI nameLabel;
    [SerializeField] Image          icon;
    [SerializeField] CanvasGroup    canvasGroup;

    private string         _itemId;
    private ItemDefinition _itemDef;

    public void Setup(string itemId, ItemDefinition def)
    {
        _itemId  = itemId;
        _itemDef = def;
        if (nameLabel) nameLabel.text = def?.name ?? itemId;
        // icon.sprite = SpriteManager.Instance.GetItemSprite(itemId) once item sprites exist
    }

    // ── Drag source ───────────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData e)
    {
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

    // ── Drop target: items dropped here move the dragged item to inventory ────

    public void OnDrop(PointerEventData _)
    {
        if (!DragDropManager.Instance.IsDragging) return;
        string dragged = DragDropManager.Instance.DraggedItemId;
        if (dragged == _itemId) return;
        DragDropManager.Instance.NotifyDropHandled();
        PlayerInfoPanel.Instance.TryMoveToInventory(dragged);
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
}
