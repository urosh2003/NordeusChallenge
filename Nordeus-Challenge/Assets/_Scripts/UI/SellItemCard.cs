using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One inventory item in the shop sell panel.
/// Prefab: nameLabel, typeLabel, sellButton.
/// </summary>
public class SellItemCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] TextMeshProUGUI nameLabel;
    [SerializeField] TextMeshProUGUI typeLabel;
    [SerializeField] Button          sellButton;

    private string          _itemId;
    private Action<string>  _onSell;

    public void Setup(string itemId, Action<string> onSell)
    {
        _itemId = itemId;
        _onSell = onSell;

        var def = GameManager.Instance.CurrentRunConfig?.GetItem(itemId);
        if (nameLabel) nameLabel.text = def?.name ?? itemId;
        if (typeLabel) typeLabel.text = def?.itemType ?? string.Empty;

        sellButton?.onClick.RemoveAllListeners();
        sellButton?.onClick.AddListener(OnSellClicked);
    }

    // ── Tooltip ───────────────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData e)
    {
        var def = GameManager.Instance.CurrentRunConfig?.GetItem(_itemId);
        if (def == null) return;

        var sb = new System.Text.StringBuilder();
        if (def.bonusStats != null)
        {
            if (def.bonusStats.health  != 0) sb.AppendLine($"Health:  {Signed(def.bonusStats.health)}");
            if (def.bonusStats.attack  != 0) sb.AppendLine($"Attack:  {Signed(def.bonusStats.attack)}");
            if (def.bonusStats.defense != 0) sb.AppendLine($"Defense: {Signed(def.bonusStats.defense)}");
            if (def.bonusStats.magic   != 0) sb.AppendLine($"Magic:   {Signed(def.bonusStats.magic)}");
        }
        Tooltip.Instance?.Show(def.name, def.itemType, sb.ToString().TrimEnd(), e.position);
    }

    public void OnPointerExit(PointerEventData _) => Tooltip.Instance?.Hide();

    // ── Private ───────────────────────────────────────────────────────────────

    private void OnSellClicked()
    {
        if (sellButton) sellButton.interactable = false;
        _onSell?.Invoke(_itemId);
    }

    private static string Signed(int v) => v >= 0 ? $"+{v}" : $"{v}";
}
