using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One purchasable offer in the shop.
/// Prefab: background Image, nameLabel, typeLabel, costLabel, buyButton, purchasedOverlay.
/// </summary>
public class ShopOfferCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image           icon;
    [SerializeField] TextMeshProUGUI nameLabel;
    [SerializeField] TextMeshProUGUI typeLabel;
    [SerializeField] TextMeshProUGUI costLabel;
    [SerializeField] Button          buyButton;
    [SerializeField] GameObject      purchasedOverlay;  // "SOLD" stamp shown after purchase

    private ShopOffer       _offer;
    private Action<ShopOffer> _onBuy;

    public void Setup(ShopOffer offer, Action<ShopOffer> onBuy, int playerGold)
    {
        _offer = offer;
        _onBuy = onBuy;

        bool isItem = offer.IsItem;
        string displayName = isItem
            ? (GameManager.Instance.CurrentRunConfig?.GetItem(offer.itemId)?.name ?? offer.itemId)
            : $"+{offer.amount} {Cap(offer.stat)}";

        string displayType = isItem
            ? (GameManager.Instance.CurrentRunConfig?.GetItem(offer.itemId)?.itemType ?? "Item")
            : "Stat Upgrade";

        if (icon)
        {
            Sprite sprite = isItem ? SpriteManager.Instance?.GetItemIcon(offer.itemId) : null;
            icon.sprite  = sprite;
            icon.enabled = sprite != null;
        }

        if (nameLabel)    nameLabel.text = displayName;
        if (typeLabel)    typeLabel.text = displayType;
        if (costLabel)    costLabel.text = $"{offer.cost}g";

        bool canAfford = playerGold >= offer.cost;
        if (buyButton)
        {
            buyButton.interactable = !offer.purchased && canAfford;
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
        }

        if (purchasedOverlay) purchasedOverlay.SetActive(offer.purchased);
    }

    public void RefreshCard(int playerGold)
    {
        if (buyButton && !_offer.purchased)
            buyButton.interactable = playerGold >= _offer.cost;
    }

    // ── Tooltip ───────────────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData e)
    {
        if (!_offer.IsItem) return;
        var def = GameManager.Instance.CurrentRunConfig?.GetItem(_offer.itemId);
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

    public void OnSold()
    {
        if (purchasedOverlay) purchasedOverlay.SetActive(_offer.purchased);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void OnBuyClicked()
    {
        if (buyButton) buyButton.interactable = false;
        _onBuy?.Invoke(_offer);
        if (purchasedOverlay) purchasedOverlay.SetActive(true);

    }

    private static string Cap(string s)    => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1).ToLower();
    private static string Signed(int v)    => v >= 0 ? $"+{v}" : $"{v}";
}
