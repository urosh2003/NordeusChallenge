using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Overlay panel shown when the player enters a SHOP node.
/// Subscribes to GameManager.OnNodeEntered and opens automatically for SHOP nodes.
/// Attach to a panel in the Play scene (MapCanvas), starts inactive.
///
/// Scene setup
/// ───────────
///   ShopPanel
///     GoldLabel          TextMeshProUGUI  — "Gold: 42"
///     StatusText         TextMeshProUGUI  — error feedback
///     OffersContainer    Transform        — GridLayout, receives ShopOfferCard prefabs
///     InventoryContainer Transform        — GridLayout, receives SellItemCard prefabs
///     LeaveButton        Button           → OnLeave()
/// </summary>
public class ShopPanel : MonoBehaviour
{
    public static ShopPanel Instance { get; private set; }

    [Header("References")]
    [SerializeField] TextMeshProUGUI statusText;
    [SerializeField] Transform       offersContainer;
    [SerializeField] Transform       inventoryContainer;
    [SerializeField] GameObject      offerCardPrefab;
    [SerializeField] GameObject      sellCardPrefab;

    private RunNode                  _currentNode;
    private readonly List<GameObject> _spawnedOffers    = new();
    private readonly List<GameObject> _spawnedSellCards = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        gameObject.SetActive(false);
        GameManager.OnNodeEntered += HandleNodeEntered;
    }

    void OnDestroy() => GameManager.OnNodeEntered -= HandleNodeEntered;

    // ── Event handler ─────────────────────────────────────────────────────────

    private void HandleNodeEntered(EnterNodeResponse response)
    {
        var node = response.run?.CurrentNode;
        if (node == null || node.Type != NodeType.SHOP) return;
        Open(node, response.player);
    }

    // ── Open / Close ──────────────────────────────────────────────────────────

    public void Open(RunNode node, PlayerStateResponse player)
    {
        _currentNode = node;
        gameObject.SetActive(true);
        SetStatus(string.Empty);
        Refresh(node, player);
    }

    public void OnLeave() => gameObject.SetActive(false);

    // ── Rebuild ───────────────────────────────────────────────────────────────

    private void Refresh(RunNode node, PlayerStateResponse player)
    {
        BuildOffers(node, player.gold);
        BuildInventory(player);
    }

    private void BuildOffers(RunNode node, int playerGold)
    {
        foreach (var go in _spawnedOffers) Destroy(go);
        _spawnedOffers.Clear();

        if (node.shopOffers == null) return;

        foreach (var offer in node.shopOffers)
        {
            var go   = Instantiate(offerCardPrefab, offersContainer);
            var card = go.GetComponent<ShopOfferCard>();
            card?.Setup(offer, OnBuyClicked, playerGold);
            _spawnedOffers.Add(go);
        }
    }

    private void BuildInventory(PlayerStateResponse player)
    {
        foreach (var go in _spawnedSellCards) Destroy(go);
        _spawnedSellCards.Clear();

        if (player.inventory == null) return;

        foreach (var itemId in player.inventory)
        {
            var go   = Instantiate(sellCardPrefab, inventoryContainer);
            var card = go.GetComponent<SellItemCard>();
            card?.Setup(itemId, OnSellClicked);
            _spawnedSellCards.Add(go);
        }
    }

    // ── Buy / Sell ────────────────────────────────────────────────────────────

    private void OnBuyClicked(ShopOffer offer)
    {
        SetStatus(string.Empty);
        GameManager.Instance.BuyShopOffer(offer.offerId,
            player =>
            {
                offer.purchased = true;
                RefreshOfferButtons(player.gold);
                BuildInventory(player);
            },
            err =>
            {
                SetStatus(err);
                RefreshOfferButtons(GameManager.Instance.PlayerState?.gold ?? 0);
            });
    }

    private void OnSellClicked(string itemId)
    {
        SetStatus(string.Empty);
        GameManager.Instance.SellItem(itemId,
            player =>
            {
                BuildInventory(player);
                RefreshOfferButtons(player.gold);
            },
            err => SetStatus(err));
    }

    private void RefreshOfferButtons(int playerGold)
    {
        foreach (var go in _spawnedOffers)
            go.GetComponent<ShopOfferCard>()?.RefreshGold(playerGold);
    }

    private void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
    }
}
