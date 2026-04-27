using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Manual API test harness. Attach to any persistent GameObject in the test scene.
/// Wire each public method to a UI Button's OnClick() in the Inspector.
///
/// Typical test flow:
///   1. CreateRun
///   2. GetRunConfig         (fetch move/item definitions — call once per run)
///   3. StartCombat          (uses encounterIndex field below)
///   3. PlayerMove_Slash  ┐
///   4. EnemyTurn         ┘ repeat until COMBAT_ENDED appears in the event log
///   5. ContinueRun          (advance past the cleared encounter)
///   6. GetPlayerState       (verify XP / level / item rewards)
///   7. SpendLevelUpPoints   (if a LEVEL_UP event fired — check HasPendingLevelUp)
///
/// Use LogLocalState at any point to compare CombatManager's event-driven
/// state against the authoritative state logged by the combat calls.
/// </summary>
public class ApiTestHarness : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Which encounter to start when clicking StartCombat (0 = first).")]
    public int encounterIndex = 0;

    [Header("SpendLevelUpPoints — must sum to 3, all >= 0")]
    public int levelUpHealth  = 1;
    public int levelUpAttack  = 1;
    public int levelUpDefense = 1;
    public int levelUpMagic   = 0;

    // ── Run ───────────────────────────────────────────────────────────────────

    public void CreateRun()
    {
        Log("CreateRun", "sending…");
        GameManager.Instance.CreateRun(
            run => LogRun("CreateRun", run),
            err => LogError("CreateRun", err));
    }

    public void GetRunConfig()
    {
        Log("GetRunConfig", "sending…");
        GameManager.Instance.GetRunConfig(
            cfg => LogRunConfig("GetRunConfig", cfg),
            err => LogError("GetRunConfig", err));
    }

    public void StartCombat()
    {
        Log("StartCombat", $"encounter {encounterIndex}…");
        GameManager.Instance.StartCombat(encounterIndex,
            combat => LogCombat("StartCombat", combat),
            err    => LogError("StartCombat", err));
    }

    public void ContinueRun()
    {
        Log("ContinueRun", "sending…");
        GameManager.Instance.ContinueRun(
            run => LogRun("ContinueRun", run),
            err => LogError("ContinueRun", err));
    }

    // ── Combat ────────────────────────────────────────────────────────────────

    public void PlayerMove_Slash()
    {
        Log("PlayerMove", "slash…");
        GameManager.Instance.PlayerAction("slash",
            combat => LogCombat("PlayerMove(slash)", combat),
            err    => LogError("PlayerMove(slash)", err));
    }

    public void EnemyTurn()
    {
        Log("EnemyTurn", "sending…");
        GameManager.Instance.TriggerEnemyTurn(
            combat => LogCombat("EnemyTurn", combat),
            err    => LogError("EnemyTurn", err));
    }

    // ── Player ────────────────────────────────────────────────────────────────

    public void GetPlayerState()
    {
        Log("GetPlayerState", "sending…");
        GameManager.Instance.GetPlayerState(
            p   => LogPlayer("GetPlayerState", p),
            err => LogError("GetPlayerState", err));
    }

    /// Spend one pending level-up using the values configured in the Inspector.
    public void SpendLevelUpPoints()
    {
        Log("SpendLevelUpPoints", $"HP:{levelUpHealth} ATK:{levelUpAttack} DEF:{levelUpDefense} MAG:{levelUpMagic}…");
        GameManager.Instance.SpendLevelUpPoints(levelUpHealth, levelUpAttack, levelUpDefense, levelUpMagic,
            p   => LogPlayer("SpendLevelUpPoints", p),
            err => LogError("SpendLevelUpPoints", err));
    }

    // ── Debug ─────────────────────────────────────────────────────────────────

    /// Logs CombatManager.LocalState — compare against the "final state" printed
    /// by combat calls to verify event-by-event mutations are correct.
    public void LogLocalState()
    {
        var state = CombatManager.Instance?.LocalState;
        if (state == null)
        {
            Debug.LogWarning("[TEST] LocalState is null — start a combat first.");
            return;
        }
        var sb = new StringBuilder();
        sb.AppendLine("[TEST] CombatManager.LocalState (event-driven)");
        AppendCharacter(sb, "  player", state.player);
        AppendCharacter(sb, "  enemy",  state.enemy);
        Debug.Log(sb.ToString());
    }

    // ── Formatters ────────────────────────────────────────────────────────────

    private static void LogRunConfig(string label, RunConfig cfg)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[TEST] {label} OK  runId:{cfg.runId}");

        sb.AppendLine($"  encounters ({cfg.encounters?.Count ?? 0}):");
        if (cfg.encounters != null)
            foreach (var enc in cfg.encounters)
            {
                sb.AppendLine($"    [{enc.index}] {enc.enemyName} lv{enc.enemyLevel}  completions:{enc.completions}");
                sb.AppendLine($"           HP:{enc.enemyMaxHp}  Mana:{enc.enemyMaxMana}(+{enc.enemyManaPerTurn})  Stamina:{enc.enemyMaxStamina}(+{enc.enemyStaminaPerTurn})");
                sb.AppendLine($"           Stats  ATK:{enc.enemyStats?.attack}  DEF:{enc.enemyStats?.defense}  MAG:{enc.enemyStats?.magic}  HP:{enc.enemyStats?.health}");
                sb.AppendLine($"           Moves  [{string.Join(", ", enc.enemyMoves ?? new List<string>())}]");
                sb.AppendLine($"           Drops  [{string.Join(", ", enc.possibleDrops ?? new List<string>())}]");
            }

        sb.AppendLine($"  moves ({cfg.moves?.Count ?? 0}):");
        if (cfg.moves != null)
            foreach (var kv in cfg.moves)
            {
                var m = kv.Value;
                var cost = m.cost != null ? $"{m.cost.costValue} {m.cost.costType}" : "free";
                sb.AppendLine($"    {m.id,-18} \"{m.name}\"  cost:{cost}");
                if (m.mainEffects != null)
                    foreach (var fx in m.mainEffects)
                    {
                        var scale = fx.scaling != null ? $" +{fx.scaling.multiplier}×{fx.scaling.stat}" : "";
                        var reduce = fx.reducedBy != null ? $" -{fx.reducedBy}" : "";
                        sb.AppendLine($"      {fx.type} {fx.target}  base:{fx.baseValue}{scale}{reduce}");
                    }
                if (m.statusEffects != null)
                    foreach (var se in m.statusEffects)
                        sb.AppendLine($"      status {se.target}  {se.type}{(se.value >= 0 ? "+" : "")}{se.value} for {se.duration} turns");
            }

        sb.AppendLine($"  items ({cfg.items?.Count ?? 0}):");
        if (cfg.items != null)
            foreach (var kv in cfg.items)
            {
                var it = kv.Value;
                sb.AppendLine($"    {it.id,-18} \"{it.name}\"  type:{it.itemType}");
                if (it.bonusStats != null)
                    sb.AppendLine($"      bonusStats  ATK:{it.bonusStats.attack}  DEF:{it.bonusStats.defense}  MAG:{it.bonusStats.magic}  HP:{it.bonusStats.health}");
                if (it.passiveEffects != null)
                    foreach (var pe in it.passiveEffects)
                        sb.AppendLine($"      passive  {pe.type}  +{pe.value}");
            }

        if (cfg.player != null)
        {
            var p = cfg.player;
            sb.AppendLine($"  player:");
            sb.AppendLine($"    known    [{string.Join(", ", p.knownMoves    ?? new List<string>())}]");
            sb.AppendLine($"    equipped [{string.Join(", ", p.equippedMoves ?? new List<string>())}]");
            sb.AppendLine($"    inventory[{string.Join(", ", p.inventory     ?? new List<string>())}]");
        }

        Debug.Log(sb.ToString());
    }

    private static void LogRun(string label, RunResponse run)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[TEST] {label} OK");
        sb.AppendLine($"  runId:   {run.runId}");
        sb.AppendLine($"  status:  {run.Status}  (raw:\"{run.status}\")");
        sb.AppendLine($"  current: encounter {run.currentEncounterIndex}");
        sb.AppendLine($"  cleared: {run.IsCompleted}");
        if (run.encounters != null)
            foreach (var enc in run.encounters)
                sb.AppendLine($"    [{enc.index}] {enc.enemyName} lv{enc.enemyLevel}  completions:{enc.completions}  cleared:{enc.IsCleared}");
        Debug.Log(sb.ToString());
    }

    private static void LogCombat(string label, CombatResponse combat)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[TEST] {label} OK");
        sb.AppendLine($"  combatId: {combat.combatId}");
        sb.AppendLine($"  events ({combat.events?.Count ?? 0}):");
        if (combat.events != null)
            foreach (var e in combat.events)
                sb.AppendLine($"    {CombatEventFormatter.Format(e)}");
        if (combat.state != null)
        {
            sb.AppendLine("  final state:");
            AppendCharacter(sb, "    player", combat.state.player);
            AppendCharacter(sb, "    enemy",  combat.state.enemy);
        }
        if (combat.playerState != null)
            AppendPlayerState(sb, "  playerState:", combat.playerState);
        Debug.Log(sb.ToString());
    }

    private static void LogPlayer(string label, PlayerStateResponse p)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[TEST] {label} OK");
        AppendPlayerState(sb, "  ", p);
        Debug.Log(sb.ToString());
    }

    private static void AppendCharacter(StringBuilder sb, string prefix, Character c)
    {
        if (c == null) { sb.AppendLine($"{prefix}: null"); return; }
        sb.AppendLine($"{prefix} {c.name} ({c.id})");
        sb.AppendLine($"{prefix}   HP      {c.currentHp} / {c.maxHp}");
        sb.AppendLine($"{prefix}   Mana    {c.currentMana} / {c.maxMana}  (+{c.manaPerTurn}/turn)");
        sb.AppendLine($"{prefix}   Stamina {c.currentStamina} / {c.maxStamina}  (+{c.staminaPerTurn}/turn)");
        sb.AppendLine($"{prefix}   Stats   ATK:{c.stats?.attack}  DEF:{c.stats?.defense}  MAG:{c.stats?.magic}  HP:{c.stats?.health}");
        sb.AppendLine($"{prefix}   Moves   [{(c.moves != null ? string.Join(", ", c.moves) : "none")}]");
    }

    private static void AppendPlayerState(StringBuilder sb, string prefix, PlayerStateResponse p)
    {
        sb.AppendLine($"{prefix}{p.characterName} (id:{p.id})  lv{p.level}");
        sb.AppendLine($"{prefix}XP       {p.currentXp} / {p.xpToNextLevel}  ({p.XpProgress:P0})");
        sb.AppendLine($"{prefix}Pending  {p.pendingStatPoints} stat points  (hasLevelUp:{p.HasPendingLevelUp})");
        sb.AppendLine($"{prefix}Stats    ATK:{p.stats?.attack}  DEF:{p.stats?.defense}  MAG:{p.stats?.magic}  HP:{p.stats?.health}");
        sb.AppendLine($"{prefix}Known    [{string.Join(", ", p.knownMoves    ?? new List<string>())}]");
        sb.AppendLine($"{prefix}Equipped [{string.Join(", ", p.equippedMoves ?? new List<string>())}]");
        sb.AppendLine($"{prefix}Inventory[{string.Join(", ", p.inventory     ?? new List<string>())}]");
        if (p.equipment != null)
        {
            var eq = p.equipment;
            sb.AppendLine($"{prefix}Equip    mainHand:{eq.mainHand ?? "—"}  offHand:{eq.offHand ?? "—"}  armor:{eq.armor ?? "—"}");
            sb.AppendLine($"{prefix}         gloves:{eq.gloves ?? "—"}  shoes:{eq.shoes ?? "—"}  amulet:{eq.amulet ?? "—"}  ring:{eq.ring ?? "—"}");
        }
    }

    private static void Log(string label, string msg)      => Debug.Log($"[TEST] {label} — {msg}");
    private static void LogError(string label, string err) => Debug.LogError($"[TEST] {label} FAILED: {err}");
}
