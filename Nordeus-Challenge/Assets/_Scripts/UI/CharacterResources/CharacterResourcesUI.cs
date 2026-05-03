using UnityEngine;

public class CharacterResourcesUI : MonoBehaviour
{
    [Header("Resource Bars")]
    [SerializeField] ResourceBarUI hpBar;
    [SerializeField] ResourceBarUI manaBar;
    [SerializeField] ResourceBarUI staminaBar;

    public TrackingCharacter trackingCharacter = TrackingCharacter.PLAYER;

    // Cached max mana/stamina from the last combat state — used as fallback if the player
    // state response predates the maxMana/maxStamina fields (should not happen in practice).
    private int _cachedMaxMana    = 0;
    private int _cachedMaxStamina = 0;

    void OnEnable()
    {
        if (trackingCharacter == TrackingCharacter.PLAYER)
        {
            CombatManager.OnPlayerChanged     += AnimateUI;
            CombatManager.OnCombatInitialized += SnapFromState;
            GameManager.OnPlayerStateUpdated  += SnapFromPlayerState;
        }
        else
        {
            CombatManager.OnEnemyChanged      += AnimateUI;
            CombatManager.OnCombatInitialized += SnapFromState;
        }

        // Sync immediately if state already exists (e.g. panel re-enabled mid-combat)
        var combatState = CombatManager.Instance?.LocalState;
        if (combatState != null)
        {
            var character = trackingCharacter == TrackingCharacter.PLAYER ? combatState.player : combatState.enemy;
            SnapUI(character);
        }
        else if (trackingCharacter == TrackingCharacter.PLAYER)
        {
            var ps = GameManager.Instance?.PlayerState;
            if (ps != null) SnapFromPlayerState(ps);
        }
    }

    void OnDisable()
    {
        if (trackingCharacter == TrackingCharacter.PLAYER)
        {
            CombatManager.OnPlayerChanged     -= AnimateUI;
            CombatManager.OnCombatInitialized -= SnapFromState;
            GameManager.OnPlayerStateUpdated  -= SnapFromPlayerState;
        }
        else
        {
            CombatManager.OnEnemyChanged      -= AnimateUI;
            CombatManager.OnCombatInitialized -= SnapFromState;
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private void AnimateUI(Character character)
    {
        if (character == null) return;
        hpBar?.AnimateTo(character.currentHp,         character.maxHp);
        manaBar?.AnimateTo(character.currentMana,      character.maxMana);
        staminaBar?.AnimateTo(character.currentStamina, character.maxStamina);
    }

    private void SnapFromState(CombatState state)
    {
        var character = trackingCharacter == TrackingCharacter.PLAYER ? state.player : state.enemy;
        if (trackingCharacter == TrackingCharacter.PLAYER && character != null)
        {
            _cachedMaxMana    = character.maxMana;
            _cachedMaxStamina = character.maxStamina;
        }
        SnapUI(character);
    }

    // Called when player state changes outside of active combat (equip, level-up, run load).
    private void SnapFromPlayerState(PlayerStateResponse ps)
    {
        if (trackingCharacter != TrackingCharacter.PLAYER) return;
        if (GameManager.Instance?.IsCombatActive == true) return;

        int maxHp      = ps.maxHp      > 0 ? ps.maxHp      : 1;
        int maxMana    = ps.maxMana    > 0 ? ps.maxMana    : _cachedMaxMana    > 0 ? _cachedMaxMana    : 1;
        int maxStamina = ps.maxStamina > 0 ? ps.maxStamina : _cachedMaxStamina > 0 ? _cachedMaxStamina : 1;

        int curHp = ps.currentHp <= 0 ? maxHp : ps.currentHp;

        hpBar?.Snap(curHp, maxHp);
        manaBar?.Snap(ps.currentMana > 0 ? ps.currentMana : maxMana, maxMana);
        staminaBar?.Snap(ps.currentStamina > 0 ? ps.currentStamina : maxStamina, maxStamina);
    }

    private void SnapUI(Character character)
    {
        if (character == null) return;
        hpBar?.Snap(character.currentHp,         character.maxHp);
        manaBar?.Snap(character.currentMana,      character.maxMana);
        staminaBar?.Snap(character.currentStamina, character.maxStamina);
    }

}
