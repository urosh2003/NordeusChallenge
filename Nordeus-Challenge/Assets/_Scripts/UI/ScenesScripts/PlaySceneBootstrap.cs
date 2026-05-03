using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages panel switching between the map overlay and the combat view.
///
/// Flow:
///   OnCombatUpdated (empty events)  → ShowCombat()
///   OnCombatEnded event fires       → sets _combatEnded flag
///   OnAllEventsProcessed            → if _combatEnded: ShowMap(); else auto-trigger enemy turn
/// </summary>
public class PlaySceneBootstrap : MonoBehaviour
{
    public static PlaySceneBootstrap Instance { get; private set; }

    [Header("Panels")]
    public GameObject mapPanel;
    public GameObject combatPanel;

    private bool _combatEnded;

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
        {
            ShowMap();
            GameManager.Instance.GetPlayerState();
        }
    }

    void OnEnable()
    {
        GameManager.OnCombatUpdated               += HandleCombatUpdated;
        CombatEventProcessor.OnCombatEnded        += HandleCombatEndedEvent;
        CombatEventProcessor.OnAllEventsProcessed += HandleAllEventsProcessed;
    }

    void OnDisable()
    {
        GameManager.OnCombatUpdated               -= HandleCombatUpdated;
        CombatEventProcessor.OnCombatEnded        -= HandleCombatEndedEvent;
        CombatEventProcessor.OnAllEventsProcessed -= HandleAllEventsProcessed;
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    // Empty events = combat just started. Mid-combat responses always have events.
    private void HandleCombatUpdated(CombatResponse r)
    {
        if (r.events == null || r.events.Count == 0)
            ShowCombat();
    }

    // Flag only — transition happens after the full queue drains so XP / gold /
    // item drop / move learnt events all play out while still on the combat panel.
    private void HandleCombatEndedEvent(CombatEvent _) => _combatEnded = true;

    private void HandleAllEventsProcessed(CombatState state, PlayerStateResponse player)
    {
        if (_combatEnded)
        {
            _combatEnded = false;
            ShowMap();
            GameManager.Instance.GetRun(); // Refresh run state to check for completion
            return;
        }

        // Auto-trigger enemy turn.
        if (GameManager.Instance.IsCombatActive && !GameManager.Instance.IsPlayerTurn)
            GameManager.Instance.TriggerEnemyTurn();
    }

    // ── Public helpers ────────────────────────────────────────────────────────

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
