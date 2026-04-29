using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages panel switching between the map overlay and the combat view.
/// Attach to any persistent object in the Play scene.
///
/// Flow:
///   OnCombatUpdated fires (StartNodeCombat response arrives) → ShowCombat()
///   OnCombatEnded fires (fight is over)                      → ShowMap()
/// </summary>
public class PlaySceneBootstrap : MonoBehaviour
{
    public static PlaySceneBootstrap Instance { get; private set; }

    [Header("Panels")]
    public GameObject mapPanel;
    public GameObject combatPanel;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentRun == null)
        {
            Debug.LogWarning("[PlayScene] No active run — returning to Main Menu.");
            SceneManager.LoadScene("MainMenu");
            return;
        }

        if (GameManager.Instance.IsCombatActive)
            ShowCombat();
        else
            ShowMap();
    }

    void OnEnable()
    {
        GameManager.OnCombatUpdated           += HandleCombatUpdated;
        CombatEventProcessor.OnCombatEnded    += HandleCombatEnded;
    }

    void OnDisable()
    {
        GameManager.OnCombatUpdated           -= HandleCombatUpdated;
        CombatEventProcessor.OnCombatEnded    -= HandleCombatEnded;
    }

    // ── Panel switching ───────────────────────────────────────────────────────

    // Empty events list = combat just started (StartNodeCombat response).
    // Mid-combat responses (player move, enemy turn) always have events, so ignore those.
    private void HandleCombatUpdated(CombatResponse r)
    {
        if (r.events == null || r.events.Count == 0)
            ShowCombat();
    }

    // Combat ended — return to map so the player can pick the next node.
    private void HandleCombatEnded(CombatEvent _) => ShowMap();

    // ── Public helpers (call from buttons if needed) ──────────────────────────

    public void ShowMap()
    {
        if (mapPanel)    mapPanel.SetActive(true);
        if (combatPanel) combatPanel.SetActive(false);
    }

    public void ShowCombat()
    {
        if (mapPanel)    mapPanel.SetActive(false);
        if (combatPanel) combatPanel.SetActive(true);
    }

    public void ReturnToMainMenu() => SceneManager.LoadScene("MainMenu");
}
