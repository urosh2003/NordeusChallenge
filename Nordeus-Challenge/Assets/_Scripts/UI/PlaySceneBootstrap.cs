using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runs once when the Play scene loads.
/// Verifies the GameManager has a valid run (guards against loading the
/// Play scene directly in the editor without going through Main Menu),
/// then opens the map overlay so the player can pick an encounter.
/// </summary>
public class PlaySceneBootstrap : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mapPanel;     // the overlay Canvas / panel — starts hidden
    public GameObject combatPanel;  // shown when a combat is active

    void Start()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentRun == null)
        {
            // Launched the Play scene directly without going through Main Menu.
            // In editor this is common; in a build it shouldn't happen.
            Debug.LogWarning("[PlayScene] No active run — returning to Main Menu.");
            SceneManager.LoadScene("MainMenu");
            return;
        }

        ShowMap();
    }

    // ── Panel helpers ─────────────────────────────────────────────────────────

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

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
