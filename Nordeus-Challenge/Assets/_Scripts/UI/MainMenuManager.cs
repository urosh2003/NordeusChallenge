using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button startNewRunButton;
    public Button continueRunButton;

    [Header("Loading indicator (optional)")]
    public GameObject loadingOverlay;

    void Start()
    {
        // Continue is only available when a run ID is saved in PlayerPrefs.
        continueRunButton.interactable = GameManager.HasSavedRun;
    }

    // ── Button callbacks ──────────────────────────────────────────────────────

    public void OnStartNewRun()
    {
        SetLoading(true);
        GameManager.Instance.CreateRun(
            run =>
            {
                // Fetch config then enter the Play scene.
                GameManager.Instance.GetRunConfig(
                    _ => SceneManager.LoadScene("PlayScene"),
                    err => OnError(err));
            },
            err => OnError(err));
    }

    public void OnContinueRun()
    {
        SetLoading(true);
        GameManager.Instance.LoadRun(GameManager.SavedRunId,
            run =>
            {
                GameManager.Instance.GetRunConfig(
                    _ => SceneManager.LoadScene("PlayScene"),
                    err => OnError(err));
            },
            err => OnError(err));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetLoading(bool on)
    {
        startNewRunButton.interactable  = !on;
        continueRunButton.interactable  = !on;
        if (loadingOverlay) loadingOverlay.SetActive(on);
    }

    private void OnError(string err)
    {
        Debug.LogError($"[MainMenu] {err}");
        SetLoading(false);
        continueRunButton.interactable = GameManager.HasSavedRun;
    }
}
