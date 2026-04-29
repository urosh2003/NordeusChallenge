using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Receives the flat list of events from a server response and plays them back
/// one at a time so the UI can animate each step (damage flash, HP bar tween, etc.)
/// instead of snapping straight to the final server state.
///
/// How to use
/// ──────────
/// 1. Attach this component to the same persistent GameObject as GameManager.
/// 2. In your combat UI scripts, subscribe to the per-event static events below.
///    Do NOT subscribe to GameManager.OnCombatUpdated for per-frame visual changes —
///    let the processor drive visuals, and use OnAllEventsProcessed for a final sync.
/// 3. Use IsProcessing to block player input while the queue is draining.
///
/// Typical combat UI subscriptions
/// ────────────────────────────────
///   CombatEventProcessor.OnDamageDealt    → flash target red, subtract from displayed HP
///   CombatEventProcessor.OnHealReceived   → flash target green, add to displayed HP
///   CombatEventProcessor.OnMoveUsed       → show move name label / trigger actor animation
///   CombatEventProcessor.OnStatusEffectApplied → show buff/debuff icon
///   CombatEventProcessor.OnTurnChanged    → switch turn indicator / enable move buttons
///   CombatEventProcessor.OnCombatEnded    → open victory / defeat panel
///   CombatEventProcessor.OnMoveLearnt     → show "New move learned!" toast
///   CombatEventProcessor.OnLevelUp        → open stat-distribution panel
///   CombatEventProcessor.OnXpGained       → animate XP bar
///   CombatEventProcessor.OnResourceRegen  → animate mana/stamina bars refilling
///   CombatEventProcessor.OnAnyEvent       → append line to the event-log scroll view
///   CombatEventProcessor.OnItemDropped        → show "Item dropped!" toast / add to inventory UI
///   CombatEventProcessor.OnAllEventsProcessed → hard-sync HP/mana/stamina bars to server truth
/// </summary>
public class CombatEventProcessor : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────

    public static CombatEventProcessor Instance { get; private set; }

    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Timing")]
    [Tooltip("Seconds to wait between events. Set to match your longest animation.")]
    public float secondsBetweenEvents = 0.7f;

    // ── Per-event static events ──────────────────────────────────────────────
    // Subscribe in Awake / OnEnable, unsubscribe in OnDestroy / OnDisable.

    /// Fired when an actor uses a move. Trigger actor animation here.
    public static event Action<CombatEvent> OnMoveUsed;

    /// Fired when damage lands. Subtract event.amount from the displayed HP of event.targetId.
    public static event Action<CombatEvent> OnDamageDealt;

    /// Fired when a heal lands. Add event.amount to the displayed HP of event.targetId.
    public static event Action<CombatEvent> OnHealReceived;

    /// Fired when a stat buff/debuff is applied.
    public static event Action<CombatEvent> OnStatusEffectApplied;

    /// Fired when the turn switches. Enable/disable move buttons based on event.newTurn.
    public static event Action<CombatEvent> OnTurnChanged;

    /// Fired when the combat ends. event.winnerId == "player" means the player won.
    public static event Action<CombatEvent> OnCombatEnded;

    /// Fired when XP is awarded. event.amount is the XP gained.
    public static event Action<CombatEvent> OnXpGained;

    /// Fired when the player learns a new move. event.moveId is the move ID.
    public static event Action<CombatEvent> OnMoveLearnt;

    /// Fired when the player levels up. Show the stat-distribution panel.
    /// event.newLevel and event.pendingStatPoints carry the new values.
    public static event Action<CombatEvent> OnLevelUp;

    /// Fired when an actor regenerates mana/stamina at the start of their turn.
    /// event.manaGained and event.staminaGained carry the amounts added.
    /// Animate mana/stamina bars refilling here before the turn-changed indicator updates.
    public static event Action<CombatEvent> OnResourceRegen;

    /// Fired when an actor pays a move's resource cost (mana / stamina / health).
    /// event.actorId, event.costType ("mana"|"stamina"|"health"), event.amount.
    /// Drain the correct bar before the damage/heal effects animate.
    public static event Action<CombatEvent> OnResourceSpent;

    /// Fired when an item drops after combat. event.itemId is the dropped item's ID.
    public static event Action<CombatEvent> OnItemDropped;

    /// Fired when gold is awarded after combat. event.amount is the gold gained.
    public static event Action<CombatEvent> OnGoldGained;

    /// Fired at end of each round when an environment effect applies to a combatant.
    /// event.targetId, event.resourceType ("HEALTH"/"MANA"/"STAMINA"), event.effectType ("GAIN"/"LOSE"), event.amount.
    public static event Action<CombatEvent> OnEnvironmentEffect;

    /// Fires for every single event — useful for appending to an event-log UI.
    public static event Action<CombatEvent> OnAnyEvent;

    /// Fires once the entire event queue is drained.
    /// The CombatState and PlayerStateResponse here are the authoritative server values —
    /// hard-sync any HP/mana bars that may have drifted during animated playback.
    public static event Action<CombatState, PlayerStateResponse> OnAllEventsProcessed;

    // ── State ────────────────────────────────────────────────────────────────

    /// True while the queue is draining. Use to block player move input.
    public bool IsProcessing { get; private set; }

    private readonly Queue<CombatEvent> _queue       = new();
    private CombatState         _pendingFinalState;
    private PlayerStateResponse _pendingPlayerState;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()  => GameManager.OnCombatUpdated += Receive;
    void OnDisable() => GameManager.OnCombatUpdated -= Receive;

    // ── Core logic ───────────────────────────────────────────────────────────

    /// Called automatically via GameManager.OnCombatUpdated.
    /// Stores the final authoritative state and queues all events for step-by-step playback.
    private void Receive(CombatResponse response)
    {
        _pendingFinalState  = response.state;
        _pendingPlayerState = response.playerState;

        if (response.events != null)
            foreach (var e in response.events)
                _queue.Enqueue(e);

        if (!IsProcessing)
            StartCoroutine(DrainQueue());
    }

    private IEnumerator DrainQueue()
    {
        IsProcessing = true;

        while (_queue.Count > 0)
        {
            var e = _queue.Dequeue();
            Dispatch(e);
            yield return new WaitForSeconds(secondsBetweenEvents);
        }

        IsProcessing = false;

        // Hard-sync UI to the authoritative server state so drift cannot accumulate.
        OnAllEventsProcessed?.Invoke(_pendingFinalState, _pendingPlayerState);
    }

    private void Dispatch(CombatEvent e)
    {
        Debug.Log($"[Event] {CombatEventFormatter.Format(e)}");
        OnAnyEvent?.Invoke(e);

        switch (e.EventType)
        {
            case CombatEventType.MOVE_USED:             OnMoveUsed?.Invoke(e);             break;
            case CombatEventType.DAMAGE_DEALT:          OnDamageDealt?.Invoke(e);          break;
            case CombatEventType.HEAL_RECEIVED:         OnHealReceived?.Invoke(e);         break;
            case CombatEventType.STATUS_EFFECT_APPLIED: OnStatusEffectApplied?.Invoke(e);  break;
            case CombatEventType.TURN_CHANGED:          OnTurnChanged?.Invoke(e);          break;
            case CombatEventType.COMBAT_ENDED:          OnCombatEnded?.Invoke(e);          break;
            case CombatEventType.XP_GAINED:             OnXpGained?.Invoke(e);             break;
            case CombatEventType.MOVE_LEARNT:           OnMoveLearnt?.Invoke(e);           break;
            case CombatEventType.LEVEL_UP:              OnLevelUp?.Invoke(e);              break;
            case CombatEventType.RESOURCE_REGEN:        OnResourceRegen?.Invoke(e);        break;
            case CombatEventType.RESOURCE_SPENT:        OnResourceSpent?.Invoke(e);        break;
            case CombatEventType.ITEM_DROPPED:          OnItemDropped?.Invoke(e);          break;
            case CombatEventType.GOLD_GAINED:           OnGoldGained?.Invoke(e);           break;
            case CombatEventType.ENVIRONMENT_EFFECT:    OnEnvironmentEffect?.Invoke(e);    break;
        }
    }
}
