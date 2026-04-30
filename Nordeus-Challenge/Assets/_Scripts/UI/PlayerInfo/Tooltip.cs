using TMPro;
using UnityEngine;

public class Tooltip : MonoBehaviour
{
    public static Tooltip Instance { get; private set; }

    [SerializeField] RectTransform   panel;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI subtitleText;
    [SerializeField] TextMeshProUGUI descriptionText;
    [SerializeField] float           gap = 16f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        panel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (panel.gameObject.activeSelf)
            UpdatePosition(Input.mousePosition);
    }

    public void Show(string title, string subtitle, string description, Vector2 screenPos)
    {
        titleText.text = title ?? string.Empty;
        SetOptional(subtitleText, subtitle);
        SetOptional(descriptionText, description);

        panel.gameObject.SetActive(true);
        UpdatePosition(screenPos);
    }

    public void Show(string title, Vector2 screenPos) =>
        Show(title, null, null, screenPos);

    public void Show(string title, string subtitle, Vector2 screenPos) =>
        Show(title, subtitle, null, screenPos);

    public void Hide() => panel.gameObject.SetActive(false);

    private void UpdatePosition(Vector2 mousePos)
    {
        Vector2 size  = panel.rect.size;
        Vector2 pivot = panel.pivot;

        // Choose side based on which half of the screen the cursor is in
        float x = (mousePos.x < Screen.width  * 0.5f)
            ? mousePos.x + gap
            : mousePos.x - gap - size.x;

        float y = (mousePos.y < Screen.height * 0.5f)
            ? mousePos.y + gap
            : mousePos.y - gap - size.y;

        // (x, y) is the bottom-left corner; adjust for panel pivot
        panel.position = new Vector2(x + size.x * pivot.x, y + size.y * pivot.y);
    }

    private void SetOptional(TextMeshProUGUI field, string value)
    {
        if (field == null) return;
        bool hasValue = !string.IsNullOrEmpty(value);
        field.gameObject.SetActive(hasValue);
        if (hasValue) field.text = value;
    }
}
