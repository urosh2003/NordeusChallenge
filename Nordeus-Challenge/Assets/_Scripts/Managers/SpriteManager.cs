using System.Collections.Generic;
using UnityEngine;

public class SpriteManager : MonoBehaviour
{
    public static SpriteManager Instance { get; private set; }

    [SerializeField] List<CharacterSO>   charactersSOs;
    [SerializeField] List<MoveSO>        movesSOs;
    [SerializeField] List<EnvironmentSO> environmentSOs;
    [SerializeField] List<ClassSO>       classSOs;

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
    public List<ClassSO> GetAllClasses()                  => classSOs ?? new List<ClassSO>();

    // ── Current combat convenience ────────────────────────────────────────────

    /// SO for the player character currently in combat (or loaded run).
    public CharacterSO PlayerSO =>
        GetCharacterSOById(GameManager.Instance?.PlayerState?.id);

    /// SO for the enemy currently in combat.
    public CharacterSO EnemySO =>
        GetCharacterSOById(CombatManager.Instance?.LocalState?.enemy?.id);

    /// SO for the active combat's environment. Null if no environment is set.
    public EnvironmentSO CurrentEnvironmentSO =>
        GetEnvironmentSO(GameManager.Instance?.CurrentEnvironmentId);

    // ── Sprite shortcuts ──────────────────────────────────────────────────────

    public Sprite PlayerSprite      => PlayerSO?.characterSprite;
    public Sprite EnemySprite       => EnemySO?.characterSprite;
    public Sprite EnvironmentSprite => CurrentEnvironmentSO?.backgroundSprite;
}
