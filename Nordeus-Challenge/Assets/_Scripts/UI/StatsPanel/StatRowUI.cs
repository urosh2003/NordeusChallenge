using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StatRowUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] TextMeshProUGUI label;
    [SerializeField] TextMeshProUGUI valueText;
    [SerializeField] TextMeshProUGUI allocatedText; // "+X" in yellow, hidden when 0
    [SerializeField] Button          plusButton;
    [SerializeField] Button          minusButton;

    private string _statName;
    private int    _base;
    private int    _equipBonus;
    private int    _allocated;

    private static readonly Dictionary<string, string> StatDescriptions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Health",  "Increases your maximum health points." },
            { "Attack",  "Increases bonus damage dealt by physical attacks, max stamina and stamina per turn." },
            { "Magic",   "Increases bonus damage dealt by magic attacks, max mana and mana per turn." },
            { "Defense", "Reduces damage taken from physical attacks." },
        };

    public void Setup(string statName, int baseValue, int equipBonus, int allocated,
                      bool canAdd, bool canRemove, Action onPlus, Action onMinus, bool disableChange = false)
    {
        _statName   = statName;
        _base       = baseValue;

        if (label) label.text = $"{statName}:";

        if (disableChange)
        {
            plusButton?.gameObject.SetActive(false);
            minusButton?.gameObject.SetActive(false);
        }
        else
        {
            plusButton?.gameObject.SetActive(true);
            minusButton?.gameObject.SetActive(true);
            plusButton?.onClick.RemoveAllListeners();
            plusButton?.onClick.AddListener(() => onPlus?.Invoke());
            minusButton?.onClick.RemoveAllListeners();
            minusButton?.onClick.AddListener(() => onMinus?.Invoke());
        }
        

        UpdateDisplay(allocated, equipBonus, canAdd, canRemove);
    }

    // Called when equipment or allocation changes — does not re-wire button callbacks.
    public void UpdateDisplay(int allocated, int equipBonus, bool canAdd, bool canRemove)
    {
        _allocated  = allocated;
        _equipBonus = equipBonus;

        int effective = _base + _allocated + _equipBonus;
        if (valueText)
        {
            valueText.text  = effective.ToString();
            valueText.color = _equipBonus > 0 ? Color.green
                            : _equipBonus < 0 ? Color.red
                            : Color.white;
        }

        if (allocatedText)
        {
            allocatedText.gameObject.SetActive(_allocated > 0);
            allocatedText.text = $"+{_allocated}";
        }

        if (plusButton)  plusButton.interactable  = canAdd;
        if (minusButton) minusButton.interactable = canRemove && _allocated > 0;
    }

    // ── Tooltip ───────────────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData e)
    {
        StatDescriptions.TryGetValue(_statName, out string description);
        string breakdown = $"Base: {_base}"
                         + (_allocated  != 0 ? $"\nPending: {Signed(_allocated)}" : "")
                         + $"\nEquipment: {Signed(_equipBonus)}"
                         + $"\nTotal: {_base + _allocated + _equipBonus}";
        Tooltip.Instance?.Show($"{_statName}:", breakdown, description, e.position);
    }

    public void OnPointerExit(PointerEventData _) => Tooltip.Instance?.Hide();

    private static string Signed(int v) => v >= 0 ? $"+{v}" : $"{v}";
}
