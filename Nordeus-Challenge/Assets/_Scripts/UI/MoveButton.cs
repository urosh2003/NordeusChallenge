using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MoveButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Icon")]
    public Image icon;
    private Button     _button;
    private MoveSO     _moveSO;
    private MoveConfig _moveConfig;

    public void Setup(string moveId, Action<string> onClick)
    {
        _button ??= GetComponent<Button>();

        _moveSO     = SpriteManager.Instance.GetMoveSOById(moveId);
        _moveConfig = GameManager.Instance.CurrentRunConfig?.GetMove(moveId);

        icon.sprite = _moveSO.moveIcon;

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => onClick?.Invoke(moveId));

        UpdateInteractable();
    }

    void OnEnable()
    {
        CombatManager.OnPlayerChanged              += OnPlayerChanged;
        CombatEventProcessor.OnTurnChanged         += OnTurnChanged;
        CombatEventProcessor.OnAllEventsProcessed  += OnAllEventsProcessed;
        UpdateInteractable();
    }

    void OnDisable()
    {
        CombatManager.OnPlayerChanged              -= OnPlayerChanged;
        CombatEventProcessor.OnTurnChanged         -= OnTurnChanged;
        CombatEventProcessor.OnAllEventsProcessed  -= OnAllEventsProcessed;
    }

    private void OnPlayerChanged(Character _)                              => UpdateInteractable();
    private void OnTurnChanged(CombatEvent _)                              => UpdateInteractable();
    private void OnAllEventsProcessed(CombatState _, PlayerStateResponse __) => UpdateInteractable();

    private void UpdateInteractable()
    {
        if (_button == null) return;
        _button.interactable = GameManager.Instance.CanPlayerAct && CanAfford();
    }

    private bool CanAfford()
    {
        var player = CombatManager.Instance?.LocalState?.player;
        if (player == null || _moveConfig?.cost == null) return true;

        return _moveConfig.cost.costType switch
        {
            "mana"    => player.currentMana    >= _moveConfig.cost.costValue,
            "stamina" => player.currentStamina >= _moveConfig.cost.costValue,
            "health"  => player.currentHp      >  _moveConfig.cost.costValue,
            _         => true
        };
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (_moveConfig == null) return;
        string title    = !string.IsNullOrEmpty(_moveConfig.name) ? _moveConfig.name : _moveConfig.id;
        string subtitle = MoveTooltipContent.Cost(_moveConfig);
        string desc     = MoveTooltipContent.Effects(_moveConfig);
        Tooltip.Instance?.Show(title, subtitle, desc, e.position);
    }

    public void OnPointerExit(PointerEventData e) => Tooltip.Instance?.Hide();
}
