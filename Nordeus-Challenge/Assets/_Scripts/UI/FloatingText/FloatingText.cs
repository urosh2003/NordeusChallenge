using System.Collections;
using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private float duration = 1f;

    private TextMeshProUGUI text;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Initialize(int amount, ResourceType resourceType, bool isGain)
    {
        if (amount == 0)
        {
            Destroy(gameObject);
            return;
        }

        // SIGN
        string sign = isGain ? "+" : "-";
        text.text = sign + Mathf.Abs(amount);
        text.fontSize = 0.3f + Mathf.Abs(amount)/100.0f;

        // COLOR
        text.color = GetColor(resourceType, isGain);

        StartCoroutine(Animate());
    }

    private Color GetColor(ResourceType type, bool isGain)
    {
        switch (type)
        {
            case ResourceType.HEALTH:
                return isGain ? Color.red : Color.white;

            case ResourceType.MANA:
                return Color.blue;

            case ResourceType.STAMINA:
                return Color.green;

            default:
                return Color.white;
        }
    }

    private IEnumerator Animate()
    {
        float t = 0f;
        Vector3 startPos = transform.localPosition;
        
        while (t < duration)
        {
            t += Time.deltaTime;

            // Move up
            transform.localPosition = startPos + Vector3.up * (t * floatSpeed);

            // Fade out
            canvasGroup.alpha = 1f - (t / duration);

            yield return null;
        }

        Destroy(gameObject);
    }
}