using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }

    [Header("Buttons")]
    public Button startNewRunButton;
    public Button continueRunButton;

    [Header("Loading indicator (optional)")]
    public GameObject loadingOverlay;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        continueRunButton.interactable = GameManager.HasSavedRun;
    }

    // ── Button callbacks ──────────────────────────────────────────────────────

    /// Called from the "New Run" button — opens class selection instead of starting directly.
    public void OnNewRunButtonClicked()
    {
        ClassSelectionPanel.Instance?.Open();
    }

    /// Called by ClassSelectionPanel after the player confirms a class.
    public void OnStartNewRun(string classId)
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
                            // No active combat — fetch player state so resource bars are correct on load.
                            GameManager.Instance.GetPlayerState(
                                _ => SceneManager.LoadScene("PlayScene"),
                                err => OnError(err));
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
    
    public void Exit() => Application.Quit();
}
