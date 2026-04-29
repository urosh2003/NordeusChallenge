using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Central singleton. Attach to a persistent GameObject in the first scene.
/// Holds all game state and exposes every backend API call.
///
/// UI scripts subscribe to the static events and call the public methods.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────

    public static GameManager Instance { get; private set; }

    // ── Config ───────────────────────────────────────────────────────────────

    [Header("Backend")]
    public string baseUrl = "http://localhost:8080/api";

    // ── Cached state ─────────────────────────────────────────────────────────

    public RunResponse         CurrentRun        { get; private set; }
    public RunConfig           CurrentRunConfig  { get; private set; }
    public string              CurrentCombatId   { get; private set; }
    public string              CurrentEnvironmentId { get; private set; }
    public CombatState         CurrentCombat     { get; private set; }
    public PlayerStateResponse PlayerState       { get; private set; }
    public bool                IsPlayerTurn      { get; private set; } = true;
    public bool                IsCombatActive    => CurrentCombatId != null && CurrentCombat != null;

    /// Looks up the current combat's environment definition from the run config. Null if none.
    public EnvironmentDefinition CurrentEnvironment =>
        CurrentRunConfig?.GetEnvironment(CurrentEnvironmentId);

    /// True when the player can legally submit a move (their turn, combat active, no events playing back).
    public bool CanPlayerAct =>
        IsPlayerTurn && IsCombatActive &&
        (CombatEventProcessor.Instance == null || !CombatEventProcessor.Instance.IsProcessing);

    // ── Events (subscribe from any UI script) ────────────────────────────────

    /// Fired once when the run config is fetched — cache move/item definitions from here.
    public static event Action<RunConfig>           OnRunConfigLoaded;

    /// Fired whenever the run map changes (create, continue, start combat updates encounter list).
    public static event Action<RunResponse>         OnRunUpdated;

    /// Fired after every combat action (player move, enemy turn, combat start).
    /// Check events list for COMBAT_ENDED, MOVE_LEARNT, LEVEL_UP, etc.
    public static event Action<CombatResponse>      OnCombatUpdated;

    /// Fired when entering a SHOP or REST_SITE node (not fired for combat nodes).
    public static event Action<EnterNodeResponse>   OnNodeEntered;

    /// Fired whenever player state changes (after victory rewards, equip, level-up spend).
    public static event Action<PlayerStateResponse> OnPlayerStateUpdated;

    /// Fired on any API error — show this to the player.
    public static event Action<string>              OnApiError;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // =========================================================================
    // RUN API
    // =========================================================================

    // ── PlayerPrefs persistence ───────────────────────────────────────────────

    private const string RunIdPrefKey = "savedRunId";

    public static bool   HasSavedRun  => PlayerPrefs.HasKey(RunIdPrefKey);
    public static string SavedRunId   => PlayerPrefs.GetString(RunIdPrefKey, null);

    private static void SaveRunId(string runId)
    {
        PlayerPrefs.SetString(RunIdPrefKey, runId);
        PlayerPrefs.Save();
    }

    public static void ClearSavedRun()
    {
        PlayerPrefs.DeleteKey(RunIdPrefKey);
        PlayerPrefs.Save();
    }

    // ── Run API ───────────────────────────────────────────────────────────────

    /// <summary>POST /api/runs — start a new run with the chosen class (e.g. "knight").</summary>
    public void CreateRun(string classId, Action<RunResponse> onSuccess = null, Action<string> onError = null)
    {
        var body = JsonUtility.ToJson(new CreateRunRequest(classId));
        StartCoroutine(HttpHelpers.Post($"{baseUrl}/runs", body,
            json => { var r = JsonUtility.FromJson<RunResponse>(json); ApplyRun(r); onSuccess?.Invoke(r); },
            err  => HandleError(err, onError)));
    }

    /// <summary>GET /api/runs/{runId} — load a run by a known ID (used by Continue Run).</summary>
    public void LoadRun(string runId, Action<RunResponse> onSuccess = null, Action<string> onError = null)
        => StartCoroutine(HttpHelpers.Get($"{baseUrl}/runs/{runId}",
            json => { var r = JsonUtility.FromJson<RunResponse>(json); ApplyRun(r); onSuccess?.Invoke(r); },
            err  => HandleError(err, onError)));

    /// <summary>
    /// GET /api/runs/{runId}/config — fetch move/item definitions for the whole run.
    /// Call once after CreateRun (or after loading a saved run). The response covers
    /// every move and item that can appear so the UI can resolve IDs locally.
    /// </summary>
    public void GetRunConfig(Action<RunConfig> onSuccess = null, Action<string> onError = null)
    {
        if (!EnsureRun(onError)) return;
        StartCoroutine(HttpHelpers.Get($"{baseUrl}/runs/{CurrentRun.runId}/config",
            json =>
            {
                var cfg = JsonConvert.DeserializeObject<RunConfig>(json);
                CurrentRunConfig = cfg;
                OnRunConfigLoaded?.Invoke(cfg);
                onSuccess?.Invoke(cfg);
            },
            err => HandleError(err, onError)));
    }

    /// <summary>GET /api/runs/{runId} — refresh the map overview.</summary>
    public void GetRun(Action<RunResponse> onSuccess = null, Action<string> onError = null)
    {
        if (!EnsureRun(onError)) return;
        StartCoroutine(HttpHelpers.Get($"{baseUrl}/runs/{CurrentRun.runId}",
            json => { var r = JsonUtility.FromJson<RunResponse>(json); ApplyRun(r); onSuccess?.Invoke(r); },
            err  => HandleError(err, onError)));
    }

    /// <summary>POST /api/runs/{runId}/nodes/{nodeId}/start — enter combat at the given map node.</summary>
    public void StartNodeCombat(string nodeId, Action<CombatResponse> onSuccess = null, Action<string> onError = null)
    {
        if (!EnsureRun(onError)) return;
        StartCoroutine(HttpHelpers.Post($"{baseUrl}/runs/{CurrentRun.runId}/nodes/{nodeId}/start", null,
            json => { var r = JsonUtility.FromJson<CombatResponse>(json); ApplyCombat(r); onSuccess?.Invoke(r); },
            err  => HandleError(err, onError)));
    }

    /// <summary>
    /// POST /api/runs/{runId}/nodes/{nodeId}/enter — enter a SHOP or REST_SITE node.
    /// Works for both the current node and an eligible next node.
    /// First visit applies the node effect (heal / generate shop offers); subsequent calls return current state.
    /// </summary>
    public void EnterNode(string nodeId, Action<EnterNodeResponse> onSuccess = null, Action<string> onError = null)
    {
        if (!EnsureRun(onError)) return;
        StartCoroutine(HttpHelpers.Post($"{baseUrl}/runs/{CurrentRun.runId}/nodes/{nodeId}/enter", null,
            json =>
            {
                var r = JsonUtility.FromJson<EnterNodeResponse>(json);
                ApplyRun(r.run);
                if (r.player != null) ApplyPlayer(r.player);
                OnNodeEntered?.Invoke(r);
                onSuccess?.Invoke(r);
            },
            err => HandleError(err, onError)));
    }

    // =========================================================================
    // COMBAT API
    // =========================================================================

    /// <summary>
    /// POST /api/combats/{combatId}/actions — submit the player's chosen move.
    /// Check OnCombatUpdated events for COMBAT_ENDED to know if the fight is over.
    /// </summary>
    public void PlayerAction(string moveId, Action<CombatResponse> onSuccess = null, Action<string> onError = null)
    {
        if (!EnsureCombat(onError)) return;
        var body = JsonUtility.ToJson(new MoveRequest(moveId));
        StartCoroutine(HttpHelpers.Post($"{baseUrl}/combats/{CurrentCombatId}/actions", body,
            json => { var r = JsonUtility.FromJson<CombatResponse>(json); ApplyCombat(r); onSuccess?.Invoke(r); },
            err  => HandleError(err, onError)));
    }

    /// <summary>
    /// GET /api/combats/{combatId}/enemy-turn — ask the server to execute the enemy's move.
    /// Call this after every TURN_CHANGED event where newTurn == "ENEMY".
    /// </summary>
    public void TriggerEnemyTurn(Action<CombatResponse> onSuccess = null, Action<string> onError = null)
    {
        if (!EnsureCombat(onError)) return;
        StartCoroutine(HttpHelpers.Get($"{baseUrl}/combats/{CurrentCombatId}/enemy-turn",
            json => { var r = JsonUtility.FromJson<CombatResponse>(json); ApplyCombat(r); onSuccess?.Invoke(r); },
            err  => HandleError(err, onError)));
    }

    /// <summary>
    /// GET /api/combats/{combatId} — restore a combat that was already in progress.
    /// Used when continuing a run that had an active fight (activeCombatId from RunResponse).
    /// </summary>
    public void LoadActiveCombat(string combatId, Action<CombatResponse> onSuccess = null, Action<string> onError = null)
        => StartCoroutine(HttpHelpers.Get($"{baseUrl}/combats/{combatId}",
            json => { var r = JsonUtility.FromJson<CombatResponse>(json); ApplyCombat(r); onSuccess?.Invoke(r); },
            err  => HandleError(err, onError)));

    // =========================================================================
    // SHOP API
    // =========================================================================

    /// <summary>POST /api/runs/{runId}/shop/buy/{offerId} — purchase a shop offer.</summary>
    public void BuyShopOffer(string offerId, Action<PlayerStateResponse> onSuccess = null, Action<string> onError = null)
    {
        if (!EnsureRun(onError)) return;
        StartCoroutine(HttpHelpers.Post($"{baseUrl}/runs/{CurrentRun.runId}/shop/buy/{offerId}", null,
            json => { var p = JsonUtility.FromJson<PlayerStateResponse>(json); ApplyPlayer(p); onSuccess?.Invoke(p); },
            err  => HandleError(err, onError)));
    }

    /// <summary>POST /api/runs/{runId}/shop/sell/{itemId} — sell an inventory item (40% base cost, min 5g).</summary>
    public void SellItem(string itemId, Action<PlayerStateResponse> onSuccess = null, Action<string> onError = null)
    {
        if (!EnsureRun(onError)) return;
        StartCoroutine(HttpHelpers.Post($"{baseUrl}/runs/{CurrentRun.runId}/shop/sell/{itemId}", null,
            json => { var p = JsonUtility.FromJson<PlayerStateResponse>(json); ApplyPlayer(p); onSuccess?.Invoke(p); },
            err  => HandleError(err, onError)));
    }

    // =========================================================================
    // PLAYER API
    // =========================================================================

    /// <summary>GET /api/runs/{runId}/player — load or refresh player state.</summary>
    public void GetPlayerState(Action<PlayerStateResponse> onSuccess = null, Action<string> onError = null)
    {
        if (!EnsureRun(onError)) return;
        StartCoroutine(HttpHelpers.Get($"{baseUrl}/runs/{CurrentRun.runId}/player",
            json => { var p = JsonUtility.FromJson<PlayerStateResponse>(json); ApplyPlayer(p); onSuccess?.Invoke(p); },
            err  => HandleError(err, onError)));
    }

    /// <summary>
    /// PUT /api/runs/{runId}/player/moves — change the 4 moves equipped for the next combat.
    /// Blocked by the server if a combat is currently active (returns 409).
    /// </summary>
    public void EquipMoves(List<string> moveIds, Action<PlayerStateResponse> onSuccess = null, Action<string> onError = null)
    {
        if (!EnsureRun(onError)) return;
        var body = JsonUtility.ToJson(new EquipMovesRequest(moveIds));
        StartCoroutine(HttpHelpers.Put($"{baseUrl}/runs/{CurrentRun.runId}/player/moves", body,
            json => { var p = JsonUtility.FromJson<PlayerStateResponse>(json); ApplyPlayer(p); onSuccess?.Invoke(p); },
            err  => HandleError(err, onError)));
    }

    /// <summary>
    /// PUT /api/runs/{runId}/player/equipment — commit the full equipment loadout at once.
    /// Pass the complete desired Equipment (null slots = empty). All previously equipped items
    /// not present in the new loadout are automatically returned to inventory by the server.
    /// Blocked during active combat (409).
    /// </summary>
    public void SetEquipment(Equipment equipment, Action<PlayerStateResponse> onSuccess = null, Action<string> onError = null)
    {
        if (!EnsureRun(onError)) return;
        var body = JsonConvert.SerializeObject(new EquipEquipmentRequest(equipment));
        StartCoroutine(HttpHelpers.Put($"{baseUrl}/runs/{CurrentRun.runId}/player/equipment", body,
            json => { var p = JsonUtility.FromJson<PlayerStateResponse>(json); ApplyPlayer(p); onSuccess?.Invoke(p); },
            err  => HandleError(err, onError)));
    }

    /// <summary>
    /// POST /api/runs/{runId}/player/level-up — spend 3 stat points from one pending level-up.
    /// health + attack + defense + magic must equal exactly 3.
    /// Call once per pending level-up (check PlayerState.pendingStatPoints).
    /// </summary>
    public void SpendLevelUpPoints(int health, int attack, int defense, int magic,
                                   Action<PlayerStateResponse> onSuccess = null, Action<string> onError = null)
    {
        if (!EnsureRun(onError)) return;
        var req = new StatDistributionRequest(health, attack, defense, magic);
        if (!req.IsValid) { HandleError("Stat points must sum to exactly 3 with no negatives.", onError); return; }

        var body = JsonUtility.ToJson(req);
        StartCoroutine(HttpHelpers.Post($"{baseUrl}/runs/{CurrentRun.runId}/player/level-up", body,
            json => { var p = JsonUtility.FromJson<PlayerStateResponse>(json); ApplyPlayer(p); onSuccess?.Invoke(p); },
            err  => HandleError(err, onError)));
    }

    // =========================================================================
    // State helpers
    // =========================================================================

    private void ApplyRun(RunResponse run)
    {
        CurrentRun = run;

        if (run.IsCompleted) ClearSavedRun();
        else                 SaveRunId(run.runId);

        OnRunUpdated?.Invoke(run);
    }

    private void ApplyCombat(CombatResponse response)
    {
        CurrentCombatId      = response.combatId;
        CurrentCombat        = response.state;
        CurrentEnvironmentId = response.environmentId;

        // Authoritative turn from server field (reliable on load/restore with empty events)
        if (!string.IsNullOrEmpty(response.currentTurn))
            IsPlayerTurn = response.currentTurn == "PLAYER";

        // Events override (mid-combat transitions)
        if (response.events != null)
        {
            foreach (var e in response.events)
            {
                if (e.EventType == CombatEventType.TURN_CHANGED)
                    IsPlayerTurn = e.newTurn == "PLAYER";

                if (e.EventType == CombatEventType.COMBAT_ENDED)
                    CurrentCombatId = null;
            }
        }

        if (response.playerState != null)
            ApplyPlayer(response.playerState);

        OnCombatUpdated?.Invoke(response);
    }

    private void ApplyPlayer(PlayerStateResponse player)
    {
        PlayerState = player;
        OnPlayerStateUpdated?.Invoke(player);
    }

    private bool EnsureRun(Action<string> onError)
    {
        if (CurrentRun == null) { HandleError("No active run. Call CreateRun first.", onError); return false; }
        return true;
    }

    private bool EnsureCombat(Action<string> onError)
    {
        if (string.IsNullOrEmpty(CurrentCombatId)) { HandleError("No active combat.", onError); return false; }
        return true;
    }

    private void HandleError(string message, Action<string> onError)
    {
        Debug.LogError($"[GameManager] {message}");
        OnApiError?.Invoke(message);
        onError?.Invoke(message);
    }

}
