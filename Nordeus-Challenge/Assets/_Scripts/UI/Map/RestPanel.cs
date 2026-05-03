using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overlay panel shown when the player enters a REST_SITE node.
/// Attach to a panel in MapCanvas, starts inactive.
///
/// Scene setup
/// ───────────
///   RestPanel
///     Background Image
///     TitleText        TextMeshProUGUI  ("A moment of rest...")
///     StatusText       TextMeshProUGUI  (← wire to statusText)
///     RestButton       Button           (← wire to restButton, OnClick → OnRestClicked)
/// </summary>
public class RestPanel : MonoBehaviour
{
    public static RestPanel Instance { get; private set; }

    [SerializeField] TextMeshProUGUI statusText;
    [SerializeField] Button          restButton;

    [SerializeField] float postRestDelay = 1.5f;

    private string _nodeId;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        gameObject.SetActive(false);
    }

    public void Open(string nodeId)
    {
        _nodeId = nodeId;
        if (statusText) statusText.text = "You find a quiet place to rest.";
        if (restButton) restButton.interactable = true;
        gameObject.SetActive(true);
    }

    public void OnRestClicked()
    {
        if (restButton) restButton.interactable = false;
        if (statusText) statusText.text = "Resting...";

        GameManager.Instance.EnterNode(_nodeId,
            onSuccess: _ => StartCoroutine(FinishRest()),
            onError: err =>
            {
                if (statusText) statusText.text = "Could not rest. Try again.";
                if (restButton) restButton.interactable = true;
                Debug.LogError($"[RestPanel] EnterNode failed: {err}");
            });
    }

    private IEnumerator FinishRest()
    {
        if (statusText) statusText.text = "You feel fully restored!";
        yield return new WaitForSeconds(postRestDelay);
        gameObject.SetActive(false);
    }
}
