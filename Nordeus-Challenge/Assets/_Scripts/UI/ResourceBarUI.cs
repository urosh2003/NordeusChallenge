using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ghost-bar resource display using Image fillAmount.
/// Both images must be set to Image Type: Filled, Fill Method: Horizontal, Fill Origin: Left.
///
/// Decrease: fillImage snaps instantly, ghostImage drains slowly.
/// Increase:  ghostImage snaps instantly, fillImage fills slowly.
/// </summary>
public class ResourceBarUI : MonoBehaviour
{
    [SerializeField] Image           fillImage;
    [SerializeField] Image           ghostImage;
    [SerializeField] TextMeshProUGUI label;
    [SerializeField] string          labelFormat  = "{0} / {1}";
    [SerializeField] float           animDuration = 0.5f;

    private Coroutine _anim;

    // ── Public API ────────────────────────────────────────────────────────────

    public void Snap(int current, int max)
    {
        StopAnim();
        float r = Ratio(current, max);
        SetFill(r);
        SetGhost(r);
        UpdateLabel(current, max);
    }

    public void AnimateTo(int current, int max)
    {
        UpdateLabel(current, max);

        float target   = Ratio(current, max);
        float fillNow  = fillImage  ? fillImage.fillAmount  : target;
        float ghostNow = ghostImage ? ghostImage.fillAmount : target;

        StopAnim();

        if (Mathf.Approximately(fillNow, target))
        {
            SetFill(target);
            SetGhost(target);
            return;
        }

        if (target < fillNow)
        {
            // Decreased: snap fill down, ghost trails behind
            SetFill(target);
            _anim = StartCoroutine(Lerp(ghostImage, ghostNow, target));
        }
        else
        {
            // Increased: snap ghost up, fill catches up
            SetGhost(target);
            _anim = StartCoroutine(Lerp(fillImage, fillNow, target));
        }
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private IEnumerator Lerp(Image img, float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed         += Time.deltaTime;
            img.fillAmount   = Mathf.Lerp(from, to, elapsed / animDuration);
            yield return null;
        }
        img.fillAmount = to;
        _anim = null;
    }

    private void StopAnim()
    {
        if (_anim != null) { StopCoroutine(_anim); _anim = null; }
    }

    private void SetFill(float r)  { if (fillImage)  fillImage.fillAmount  = r; }
    private void SetGhost(float r) { if (ghostImage) ghostImage.fillAmount = r; }

    private void UpdateLabel(int current, int max)
    {
        if (label) label.text = string.Format(labelFormat, current, max);
    }

    private static float Ratio(int current, int max) =>
        max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
}
