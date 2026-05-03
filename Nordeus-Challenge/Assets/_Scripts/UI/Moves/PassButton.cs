using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach to a Button in the combat HUD to let the player skip their turn.
/// Mirrors the enable/disable logic of MoveButton — interactable only when
/// GameManager.CanPlayerAct is true (player's turn, combat active, no events playing).
/// </summary>
public class PassButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Button _button;

    void Awake() => _button = GetComponent<Button>();

    void OnEnable()
    {
        _button.onClick.AddListener(OnClick);
        CombatManager.OnPlayerChanged             += OnPlayerChanged;
        CombatEventProcessor.OnTurnChanged        += OnTurnChanged;
        CombatEventProcessor.OnAllEventsProcessed += OnAllEventsProcessed;
        UpdateInteractable();
    }

    void OnDisable()
    {
        _button.onClick.RemoveListener(OnClick);
        CombatManager.OnPlayerChanged             -= OnPlayerChanged;
        CombatEventProcessor.OnTurnChanged        -= OnTurnChanged;
        CombatEventProcessor.OnAllEventsProcessed -= OnAllEventsProcessed;
    }

    private void OnPlayerChanged(Character _)                               => UpdateInteractable();
    private void OnTurnChanged(CombatEvent _)                               => UpdateInteractable();
    private void OnAllEventsProcessed(CombatState _, PlayerStateResponse __) => UpdateInteractable();

    private void OnClick()
    {
        GameManager.Instance.PlayerPass(
            _ => Debug.Log("[PassButton] Pass OK"),
            err => Debug.LogError($"[PassButton] {err}"));
    }

    private void UpdateInteractable()
    {
        if (_button != null)
            _button.interactable = GameManager.Instance?.CanPlayerAct ?? false;
    }
    
    public void OnPointerEnter(PointerEventData e)
    {
        string title    = "Pass";
        string subtitle = "Free!";
        string desc     = "Do nothing, passing the turn to your opponent.";
        Tooltip.Instance?.Show(title, subtitle, desc, e.position);
    }

    public void OnPointerExit(PointerEventData e) => Tooltip.Instance?.Hide();
}
