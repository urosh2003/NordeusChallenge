using System;
using UnityEngine;

/// <summary>
/// Maintains a local copy of CombatState and updates it event-by-event as
/// CombatEventProcessor plays back each server event, replicating the backend
/// mutations so the UI can animate to the correct intermediate values.
///
/// LocalState starts at the previous turn's final state. Each event mutates it
/// one step at a time. OnAllEventsProcessed does a hard-sync to server truth,
/// which also corrects any drift (e.g. move costs that have no dedicated event).
///
/// UI scripts should subscribe to OnPlayerChanged / OnEnemyChanged to update
/// bars and labels progressively, and to OnCombatInitialized to set up the
/// initial UI when a new combat starts.
/// </summary>
public class CombatManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────

    public static CombatManager Instance { get; private set; }

    // ── State ────────────────────────────────────────────────────────────────

    /// Local combat state, mutated event-by-event during playback.
    public CombatState LocalState { get; private set; }

    // ── Events ───────────────────────────────────────────────────────────────

    /// Fired when a fresh combat is started (LocalState is the initial state).
    public static event Action<CombatState> OnCombatInitialized;

    /// Fired whenever the player character's state changes during event playback.
    public static event Action<Character> OnPlayerChanged;

    /// Fired whenever the enemy character's state changes during event playback.
    public static event Action<Character> OnEnemyChanged;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        GameManager.OnCombatUpdated                  += HandleCombatUpdated;
        CombatEventProcessor.OnDamageDealt           += HandleDamage;
        CombatEventProcessor.OnHealReceived          += HandleHeal;
        CombatEventProcessor.OnStatusEffectApplied   += HandleStatusEffect;
        CombatEventProcessor.OnStatusEffectExpired   += HandleStatusEffectExpired;
        CombatEventProcessor.OnResourceRegen         += HandleResourceRegen;
        CombatEventProcessor.OnResourceSpent         += HandleResourceSpent;
        CombatEventProcessor.OnEnvironmentEffect     += HandleEnvironmentEffect;
        CombatEventProcessor.OnAllEventsProcessed    += HandleHardSync;
    }

    void OnDisable()
    {
        GameManager.OnCombatUpdated                  -= HandleCombatUpdated;
        CombatEventProcessor.OnDamageDealt           -= HandleDamage;
        CombatEventProcessor.OnHealReceived          -= HandleHeal;
        CombatEventProcessor.OnStatusEffectApplied   -= HandleStatusEffect;
        CombatEventProcessor.OnStatusEffectExpired   -= HandleStatusEffectExpired;
        CombatEventProcessor.OnResourceRegen         -= HandleResourceRegen;
        CombatEventProcessor.OnResourceSpent         -= HandleResourceSpent;
        CombatEventProcessor.OnEnvironmentEffect     -= HandleEnvironmentEffect;
        CombatEventProcessor.OnAllEventsProcessed    -= HandleHardSync;
    }

    // ── Combat response handling ──────────────────────────────────────────────

    private void HandleCombatUpdated(CombatResponse response)
    {
        // When there are no events (StartCombat), initialize immediately.
        // When events exist, keep LocalState as-is so event handlers animate from here.
        if (response.events == null || response.events.Count == 0)
            InitializeState(response.state);
    }

    private void InitializeState(CombatState state)
    {
        LocalState = state;
        OnCombatInitialized?.Invoke(state);
        OnPlayerChanged?.Invoke(state.player);
        OnEnemyChanged?.Invoke(state.enemy);
    }

    // ── Hard sync ─────────────────────────────────────────────────────────────

    // Replaces LocalState with the authoritative server state after all events
    // have played back. This corrects any drift (e.g. missing move cost events).
    private void HandleHardSync(CombatState finalState, PlayerStateResponse _)
    {
        LocalState = finalState;
        OnPlayerChanged?.Invoke(finalState.player);
        OnEnemyChanged?.Invoke(finalState.enemy);
    }

    // ── Event-driven state mutations ──────────────────────────────────────────

    // target.currentHp = max(0, currentHp - amount)
    private void HandleDamage(CombatEvent e)
    {
        var target = ResolveCharacter(e.targetId);
        if (target == null) return;

        target.currentHp = Mathf.Max(0, target.currentHp - e.amount);
        NotifyChanged(target);
    }

    // target.currentHp = min(maxHp, currentHp + amount)
    // amount is already capped by the server (min(value, maxHp - currentHp))
    private void HandleHeal(CombatEvent e)
    {
        var target = ResolveCharacter(e.targetId);
        if (target == null) return;

        target.currentHp = Mathf.Min(target.maxHp, target.currentHp + e.amount);
        NotifyChanged(target);
    }

    // target.stats[statType] += value  (negative value = debuff)
    // Note: changing attack/magic also changes maxStamina/maxMana on the backend,
    // but we can't recompute those without bonus data — hard-sync will correct them.
    private void HandleStatusEffect(CombatEvent e)
    {
        var target = ResolveCharacter(e.targetId);
        if (target == null) return;

        switch (e.statType)
        {
            case "attack":  target.stats.attack  += e.value; break;
            case "defense": target.stats.defense += e.value; break;
            case "magic":   target.stats.magic   += e.value; break;
        }
        NotifyChanged(target);
    }

    // Reverts the stat change when an effect expires.
    private void HandleStatusEffectExpired(CombatEvent e)
    {
        var target = ResolveCharacter(e.targetId);
        if (target == null) return;

        switch (e.statType)
        {
            case "attack":  target.stats.attack  -= e.value; break;
            case "defense": target.stats.defense -= e.value; break;
            case "magic":   target.stats.magic   -= e.value; break;
        }
        NotifyChanged(target);
    }

    // actor.current[costType] -= amount
    private void HandleResourceSpent(CombatEvent e)
    {
        var actor = ResolveCharacter(e.actorId);
        if (actor == null) return;

        switch (e.costType)
        {
            case "mana":    actor.currentMana    -= e.amount; break;
            case "stamina": actor.currentStamina -= e.amount; break;
            case "health":  actor.currentHp       = Mathf.Max(0, actor.currentHp - e.amount); break;
        }
        NotifyChanged(actor);
    }

    // target.currentMana    += manaGained    (already capped by server)
    // target.currentStamina += staminaGained (already capped by server)
    private void HandleResourceRegen(CombatEvent e)
    {
        var target = ResolveCharacter(e.targetId);
        if (target == null) return;

        target.currentMana    = Mathf.Min(target.maxMana,    target.currentMana    + e.manaGained);
        target.currentStamina = Mathf.Min(target.maxStamina, target.currentStamina + e.staminaGained);
        NotifyChanged(target);
    }

    private void HandleEnvironmentEffect(CombatEvent e)
    {
        var target = ResolveCharacter(e.targetId);
        if (target == null) return;

        switch (e.resourceType)
        {
            case "HEALTH":
                target.currentHp      = Mathf.Clamp(target.currentHp      + e.amount, 0, target.maxHp);
                break;
            case "MANA":
                target.currentMana    = Mathf.Clamp(target.currentMana    + e.amount, 0, target.maxMana);
                break;
            case "STAMINA":
                target.currentStamina = Mathf.Clamp(target.currentStamina + e.amount, 0, target.maxStamina);
                break;
        }
        NotifyChanged(target);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Character ResolveCharacter(string characterId)
    {
        if (LocalState == null) return null;
        if (LocalState.player?.id == characterId) return LocalState.player;
        if (LocalState.enemy?.id  == characterId) return LocalState.enemy;
        return null;
    }

    private void NotifyChanged(Character character)
    {
        if (LocalState == null) return;
        if (character == LocalState.player) OnPlayerChanged?.Invoke(character);
        else                                OnEnemyChanged?.Invoke(character);
    }
}
