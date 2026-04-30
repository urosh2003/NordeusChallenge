using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach inside CombatPanel. Reads the active EnvironmentSO from SpriteManager
/// and updates the background image and name label whenever combat starts or is restored.
///
/// Scene setup
/// ───────────
///   EnvironmentDisplay (this script)
///     BackgroundImage   Image   — full-panel background
///     EnvironmentLabel  TextMeshProUGUI (optional) — shows environment name
/// </summary>
public class EnvironmentDisplay : MonoBehaviour
{
    [SerializeField] Image           backgroundImage;
    [SerializeField] TextMeshProUGUI environmentLabel;

    void OnEnable()  => GameManager.OnCombatUpdated += HandleCombatUpdated;
    void OnDisable() => GameManager.OnCombatUpdated -= HandleCombatUpdated;

    void Start() => Refresh();

    private void HandleCombatUpdated(CombatResponse r)
    {
        // Only refresh on combat-start responses (empty events = new combat)
        if (r.events == null || r.events.Count == 0)
            Refresh();
    }

    private void Refresh()
    {
        var so = SpriteManager.Instance?.CurrentEnvironmentSO;
        if (backgroundImage)   backgroundImage.sprite = so?.backgroundSprite;
        if (environmentLabel)  environmentLabel.text  = so?.displayName ?? string.Empty;
    }
}
