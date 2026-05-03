using UnityEngine;
using UnityEngine.EventSystems;

public class MoveDropZone : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData _)
    {
        if (!DragDropManager.Instance.IsDragging) return;
        string dragged = DragDropManager.Instance.DraggedItemId;
        DragDropManager.Instance.NotifyDropHandled();
        DragDropManager.Instance.EndDrag();
        MovesPanel.Instance.TryMoveToKnown(dragged);
    }
}
