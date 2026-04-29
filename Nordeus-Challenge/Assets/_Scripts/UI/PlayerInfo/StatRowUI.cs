using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class StatRowUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] TextMeshProUGUI label;
    [SerializeField] TextMeshProUGUI valueText;

    private int _base;
    private int _equipBonus;

    public void Setup(string statName, int baseValue, int equipBonus)
    {
        _base       = baseValue;
        _equipBonus = equipBonus;

        if (label)     label.text     = statName;
        if (valueText) valueText.text = (baseValue + equipBonus).ToString();
    }

    public void OnPointerEnter(PointerEventData e)
    {
        string breakdown = $"Base (Lv): {_base}\nEquipment: {Signed(_equipBonus)}\nTotal: {_base + _equipBonus}";
        Tooltip.Instance?.Show(label.text, breakdown, e.position);
    }

    public void OnPointerExit(PointerEventData _) => Tooltip.Instance?.Hide();

    private static string Signed(int v) => v >= 0 ? $"+{v}" : $"{v}";
}
