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

    public void OnStartNewRun(string classId = "knight")
    {
        SetLoading(true);
        GameManager.Instance.CreateRun(classId,
            run =>
            {
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
                    _ =>
                    {
                        if (!string.IsNullOrEmpty(run.activeCombatId))
                        {
                            // Restore in-progress combat before entering the play scene
                            // so PlaySceneBootstrap can show the combat panel directly.
                            GameManager.Instance.LoadActiveCombat(run.activeCombatId,
                                _ => SceneManager.LoadScene("PlayScene"),
                                err => OnError(err));
                        }
                        else
                        {
                            SceneManager.LoadScene("PlayScene");
                        }
                    },
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
