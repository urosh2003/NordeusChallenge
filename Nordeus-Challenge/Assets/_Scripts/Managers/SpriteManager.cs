using System.Collections.Generic;
using UnityEngine;

public class SpriteManager : MonoBehaviour
{
    public static SpriteManager Instance { get; private set; }

    [SerializeField] List<CharacterSO>   charactersSOs;
    [SerializeField] List<MoveSO>        movesSOs;
    [SerializeField] List<EnvironmentSO> environmentSOs;
    [SerializeField] List<ClassSO>       classSOs;
    [SerializeField] List<ItemSO>        itemSOs;

    [Header("Stat Type Icons")]
    [SerializeField] Sprite attackPositive;
    [SerializeField] Sprite attackNegative;
    [SerializeField] Sprite defensePositive;
    [SerializeField] Sprite defenseNegative;
    [SerializeField] Sprite magicPositive;
    [SerializeField] Sprite magicNegative;
    [SerializeField] Sprite bleed;
    [SerializeField] Sprite poison;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Lookup by ID ──────────────────────────────────────────────────────────

    public CharacterSO   GetCharacterSOById(string id)   => charactersSOs?.Find(so => so.characterId   == id);
    public MoveSO        GetMoveSOById(string id)         => movesSOs?.Find(so => so.moveId             == id);
    public EnvironmentSO GetEnvironmentSO(string id)      => environmentSOs?.Find(so => so.environmentId == id);
    public ClassSO       GetClassSO(string id)            => classSOs?.Find(so => so.classId            == id);
    public ItemSO        GetItemSOById(string id)         => itemSOs?.Find(so => so.itemId              == id);
    public List<ClassSO>       GetAllClasses()         => classSOs ?? new List<ClassSO>();
    public List<EnvironmentSO> GetAllEnvironmentSOs()  => environmentSOs ?? new List<EnvironmentSO>();
    public Sprite        GetItemIcon(string id)           => GetItemSOById(id)?.icon;

    public Sprite GetStatTypeIcon(string statType, int value) => (statType, value >= 0) switch
    {
        ("attack",  true)  => attackPositive,
        ("attack",  false) => attackNegative,
        ("defense", true)  => defensePositive,
        ("defense", false) => defenseNegative,
        ("magic",   true)  => magicPositive,
        ("magic",   false) => magicNegative,
        ("bleed",   true)  => bleed,
        ("bleed",   false) => bleed,
        ("poison",  true)  => poison,
        ("poison",  false) => poison,
        _                  => null
    };

    // ── Current combat convenience ────────────────────────────────────────────

    /// SO for the player character currently in combat (or loaded run).
    public CharacterSO PlayerSO =>
        GetCharacterSOById(GameManager.Instance?.PlayerState?.id);

    /// SO for the enemy currently in combat.
    public CharacterSO EnemySO =>
        GetCharacterSOById(CombatManager.Instance?.LocalState?.enemy?.id);

    // ── Sprite shortcuts ──────────────────────────────────────────────────────

    public Sprite PlayerSprite => PlayerSO?.characterSprite;
    public Sprite EnemySprite  => EnemySO?.characterSprite;
}
