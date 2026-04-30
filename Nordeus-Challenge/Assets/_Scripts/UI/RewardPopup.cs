using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Modal popup that appears (one at a time) for MOVE_LEARNT and ITEM_DROPPED events.
/// Pauses CombatEventProcessor while visible so the queue waits for the player to dismiss.
///
/// Scene setup (combat canvas, starts inactive)
/// ────────────────────────────────────────────
///   RewardPopup (this script, full-screen semi-transparent overlay)
///     TitleText       TextMeshProUGUI  — "Move Learned!" / "Item Dropped!"
///     NameText        TextMeshProUGUI  — name of the move/item
///     DescText        TextMeshProUGUI  — stats / description
///     OkButton        Button           → OnOk()
/// </summary>
public class RewardPopup : MonoBehaviour
{
    public static RewardPopup Instance { get; private set; }

    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI descText;
    [SerializeField] Button          okButton;

    private readonly Queue<(string title, string name, string desc)> _pending = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        gameObject.SetActive(false);

        // Awake (not OnEnable) so the subscription works even while inactive.
        CombatEventProcessor.OnMoveLearnt  += HandleMoveLearnt;
        CombatEventProcessor.OnItemDropped += HandleItemDropped;
    }

    void OnDestroy()
    {
        CombatEventProcessor.OnMoveLearnt  -= HandleMoveLearnt;
        CombatEventProcessor.OnItemDropped -= HandleItemDropped;
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void HandleMoveLearnt(CombatEvent e)
    {
        var cfg  = GameManager.Instance.CurrentRunConfig;
        var move = cfg?.GetMove(e.moveId);
        Enqueue("Move Learned!", move?.name ?? e.moveId, BuildMoveDesc(move));
    }

    private void HandleItemDropped(CombatEvent e)
    {
        var cfg  = GameManager.Instance.CurrentRunConfig;
        var item = cfg?.GetItem(e.itemId);
        Enqueue("Item Dropped!", item?.name ?? e.itemId, BuildItemDesc(item));
    }

    // ── OK button (wire in Inspector) ─────────────────────────────────────────

    public void OnOk()
    {
        if (_pending.Count > 0)
            ShowNext();
        else
        {
            gameObject.SetActive(false);
            CombatEventProcessor.Instance?.Resume();
        }
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void Enqueue(string title, string name, string desc)
    {
        CombatEventProcessor.Instance?.Pause();
        _pending.Enqueue((title, name, desc));
        if (!gameObject.activeSelf)
            ShowNext();
    }

    private void ShowNext()
    {
        var (title, name, desc) = _pending.Dequeue();
        if (titleText) titleText.text = title;
        if (nameText)  nameText.text  = name;
        if (descText)  descText.text  = desc;
        gameObject.SetActive(true);
    }

    private static string BuildMoveDesc(MoveConfig move)
    {
        if (move == null) return string.Empty;
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(move.description))
            sb.AppendLine(move.description);
        if (move.cost != null && move.cost.costType != "none")
            sb.Append($"Cost: {move.cost.costValue} {move.cost.costType}");
        return sb.ToString().TrimEnd();
    }

    private static string BuildItemDesc(ItemDefinition item)
    {
        if (item == null) return string.Empty;
        var sb  = new StringBuilder();
        var b   = item.bonusStats;
        if (b != null)
        {
            if (b.health  != 0) sb.AppendLine($"Health:  {Signed(b.health)}");
            if (b.attack  != 0) sb.AppendLine($"Attack:  {Signed(b.attack)}");
            if (b.defense != 0) sb.AppendLine($"Defense: {Signed(b.defense)}");
            if (b.magic   != 0) sb.AppendLine($"Magic:   {Signed(b.magic)}");
        }
        if (!string.IsNullOrEmpty(item.description))
            sb.Append(item.description);
        return sb.ToString().TrimEnd();
    }

    private static string Signed(int v) => v >= 0 ? $"+{v}" : $"{v}";
}
