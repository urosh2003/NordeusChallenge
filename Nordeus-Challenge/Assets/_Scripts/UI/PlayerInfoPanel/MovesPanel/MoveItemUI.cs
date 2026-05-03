using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MoveItemUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image            icon;
    [SerializeField] TextMeshProUGUI  nameLabel;
    [SerializeField] CanvasGroup      canvasGroup;

    private string     _moveId;
    private MoveConfig _moveDef;

    public void Setup(string moveId, MoveConfig def)
    {
        _moveId  = moveId;
        _moveDef = def;

        if (nameLabel) nameLabel.text = def?.name ?? moveId;
        if (icon)
        {
            var so = SpriteManager.Instance?.GetMoveSOById(moveId);
            if (so != null) icon.sprite = so.moveIcon;
        }
    }

    // ── Drag source ───────────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData e)
    {
        if (!MovesPanel.Instance.IsEditable) return;
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

    // ── Drop target: moves dropped here return to the known pool ──────────────

    public void OnDrop(PointerEventData _)
    {
        if (!DragDropManager.Instance.IsDragging) return;
        string dragged = DragDropManager.Instance.DraggedItemId;
        if (dragged == _moveId) return;
        DragDropManager.Instance.NotifyDropHandled();
        DragDropManager.Instance.EndDrag();
        MovesPanel.Instance.TryMoveToKnown(dragged);
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
