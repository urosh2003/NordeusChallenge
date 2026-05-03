using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MoveSlotUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot")]
    public int slotIndex;

    [Header("References")]
    [SerializeField] Image            icon;
    [SerializeField] GameObject       emptyOverlay;
    [SerializeField] CanvasGroup      canvasGroup;
    [SerializeField] TextMeshProUGUI  slotLabel;

    private string     _moveId;
    private MoveConfig _moveDef;

    public void Refresh(string moveId, MoveConfig def)
    {
        _moveId  = moveId;
        _moveDef = def;

        bool filled = !string.IsNullOrEmpty(moveId);
        if (emptyOverlay) emptyOverlay.SetActive(!filled);
        if (icon)
        {
            icon.enabled = filled;
            if (filled)
            {
                var so = SpriteManager.Instance?.GetMoveSOById(moveId);
                if (so != null) icon.sprite = so.moveIcon;
            }
        }
    }

    // ── Drag source ───────────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData e)
    {
        if (string.IsNullOrEmpty(_moveId) || !MovesPanel.Instance.IsEditable) return;
        if (canvasGroup) canvasGroup.blocksRaycasts = false;
        var so = SpriteManager.Instance?.GetMoveSOById(_moveId);
        DragDropManager.Instance.BeginDrag(_moveId, so?.moveIcon, e.position);
    }

    public void OnDrag(PointerEventData e) => DragDropManager.Instance.UpdateGhost(e.position);

    public void OnEndDrag(PointerEventData _)
    {
        if (canvasGroup) canvasGroup.blocksRaycasts = true;
        DragDropManager.Instance.EndDrag();
        if (!DragDropManager.Instance.WasDropHandled)
            MovesPanel.Instance.Refresh();
    }

    // ── Drop target ───────────────────────────────────────────────────────────

    public void OnDrop(PointerEventData _)
    {
        if (!DragDropManager.Instance.IsDragging) return;
        string dragged = DragDropManager.Instance.DraggedItemId;
        DragDropManager.Instance.NotifyDropHandled();
        DragDropManager.Instance.EndDrag();
        MovesPanel.Instance.TryMoveToSlot(dragged, slotIndex);
    }

    // ── Tooltip ───────────────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData e)
    {
        if (_moveDef != null)
            Tooltip.Instance?.Show(_moveDef.name, MoveTooltipContent.Cost(_moveDef),
                MoveTooltipContent.Effects(_moveDef), e.position);
    }

    public void OnPointerExit(PointerEventData _) => Tooltip.Instance?.Hide();
}
