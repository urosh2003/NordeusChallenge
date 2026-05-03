using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Displays active status effects for one combatant as a row of icon cells.
/// One instance per combatant — set trackingCharacter in the Inspector.
///
/// Scene setup
/// ───────────
///   StatusEffectDisplay (this script)
///     Container   HorizontalLayoutGroup  ← wire to container
///   iconPrefab   StatusEffectIconUI prefab
/// </summary>
public class StatusEffectDisplay : MonoBehaviour
{
    [SerializeField] TrackingCharacter trackingCharacter = TrackingCharacter.PLAYER;
    [SerializeField] Transform         container;
    [SerializeField] GameObject        iconPrefab;

    private readonly List<StatusEffectIconUI> _icons = new();
    private string _trackedId;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void OnEnable()
    {
        CombatManager.OnCombatInitialized          += OnCombatInitialized;
        CombatEventProcessor.OnStatusEffectApplied += OnApplied;
        CombatEventProcessor.OnStatusEffectExpired += OnExpired;
        CombatEventProcessor.OnTurnChanged         += OnTurnChanged;
        CombatEventProcessor.OnAllEventsProcessed  += OnHardSync;
        CombatEventProcessor.OnCombatEnded += OnCombatEnded;

        var state = CombatManager.Instance?.LocalState;
        if (state != null) OnCombatInitialized(state);
    }

    void OnDisable()
    {
        CombatManager.OnCombatInitialized          -= OnCombatInitialized;
        CombatEventProcessor.OnStatusEffectApplied -= OnApplied;
        CombatEventProcessor.OnStatusEffectExpired -= OnExpired;
        CombatEventProcessor.OnTurnChanged         -= OnTurnChanged;
        CombatEventProcessor.OnAllEventsProcessed  -= OnHardSync;
        CombatEventProcessor.OnCombatEnded -= OnCombatEnded;

    }

    // ── Event handlers ────────────────────────────────────────────────────────
    private void OnCombatEnded(CombatEvent combatEvent)
    {
        _icons.Clear();
    }
    
    private void OnCombatInitialized(CombatState state)
    {
        var character = trackingCharacter == TrackingCharacter.PLAYER ? state.player : state.enemy;
        _trackedId = character?.id;
        RebuildFromList(character?.activeEffects);
    }

    private void OnApplied(CombatEvent e)
    {
        if (e.targetId != _trackedId) return;

        if (e.stacked)
        {
            var existing = _icons.Find(i => i.Effect.effectType == e.statType);
            if (existing != null) { existing.UpdateEffect(e.value, e.duration); return; }
        }

        SpawnIcon(new ActiveEffectInfo { effectType = e.statType, value = e.value, duration = e.duration });
    }

    private void OnExpired(CombatEvent e)
    {
        if (e.targetId != _trackedId) return;
        var match = _icons.Find(i => i.Effect.effectType == e.statType && i.Effect.value == e.value);
        if (match == null) return;
        _icons.Remove(match);
        Destroy(match.gameObject);
    }

    private void OnTurnChanged(CombatEvent e)
    {
        // Tick durations when OUR character's turn just ended
        bool ourTurnEnded = (trackingCharacter == TrackingCharacter.PLAYER && e.newTurn == "ENEMY")
                         || (trackingCharacter == TrackingCharacter.ENEMY  && e.newTurn == "PLAYER");
        if (!ourTurnEnded) return;
        foreach (var icon in _icons)
            icon.SetDuration(icon.Effect.duration - 1);
    }

    private void OnHardSync(CombatState state, PlayerStateResponse _)
    {
        var character = trackingCharacter == TrackingCharacter.PLAYER ? state?.player : state?.enemy;
        _trackedId = character?.id;
        RebuildFromList(character?.activeEffects);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void RebuildFromList(List<ActiveEffectInfo> effects)
    {
        foreach (var icon in _icons) Destroy(icon.gameObject);
        _icons.Clear();

        if (effects == null) return;
        foreach (var effect in effects) SpawnIcon(effect);
    }

    private void SpawnIcon(ActiveEffectInfo effect)
    {
        var go   = Instantiate(iconPrefab, container);
        var icon = go.GetComponent<StatusEffectIconUI>();
        icon?.Setup(effect);
        _icons.Add(icon);
    }
}
