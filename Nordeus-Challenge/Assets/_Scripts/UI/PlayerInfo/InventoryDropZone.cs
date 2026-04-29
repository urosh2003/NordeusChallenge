using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to the inventory panel background so items dragged from equipment slots
/// can be dropped onto the empty area (not just onto existing inventory items).
/// </summary>
public class InventoryDropZone : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData _)
    {
        if (!DragDropManager.Instance.IsDragging) return;
        string dragged = DragDropManager.Instance.DraggedItemId;
        DragDropManager.Instance.NotifyDropHandled();
        DragDropManager.Instance.EndDrag();
        PlayerInfoPanel.Instance.TryMoveToInventory(dragged);
    }
}
