using UnityEngine;
using UnityEngine.UI;

public class DragDropManager : MonoBehaviour
{
    public static DragDropManager Instance { get; private set; }

    [SerializeField] Canvas rootCanvas;
    [SerializeField] Image  ghostImage;

    public string DraggedItemId   { get; private set; }
    public bool   IsDragging      { get; private set; }
    public bool   WasDropHandled  { get; private set; }

    private bool _dropHandled;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        ghostImage.gameObject.SetActive(false);
        ghostImage.raycastTarget = false;
    }

    public void BeginDrag(string itemId, Sprite icon, Vector2 screenPos)
    {
        DraggedItemId  = itemId;
        IsDragging     = true;
        _dropHandled   = false;
        WasDropHandled = false;

        ghostImage.sprite = icon;
        ghostImage.color  = icon != null ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        ghostImage.gameObject.SetActive(true);
        MoveGhost(screenPos);
    }

    public void UpdateGhost(Vector2 screenPos)
    {
        if (IsDragging) MoveGhost(screenPos);
    }

    public void NotifyDropHandled() => _dropHandled = true;

    /// Hides the ghost and clears drag state. Safe to call multiple times.
    public void EndDrag()
    {
        ghostImage.gameObject.SetActive(false);
        if (!IsDragging) return;

        IsDragging     = false;
        WasDropHandled = _dropHandled;
        DraggedItemId  = null;
        _dropHandled   = false;
    }

    private void MoveGhost(Vector2 screenPos)
    {
        var rt     = rootCanvas.GetComponent<RectTransform>();
        var camera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, camera, out var local))
            ghostImage.rectTransform.anchoredPosition = local;
    }
}
