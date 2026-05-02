using System;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSpriteManager : MonoBehaviour
{
    public TrackingCharacter trackingCharacter = TrackingCharacter.PLAYER;
    [SerializeField] private SpriteRenderer playerSprite;
    [SerializeField] private SpriteRenderer enemySprite;
    [SerializeField] private Image playerAvatar;
    [SerializeField] private Image enemyAvatar;

    void Start()
    {
        CombatManager.OnCombatInitialized += refreshEnemySprites;
        
        playerAvatar.sprite = SpriteManager.Instance.GetCharacterSOById(GameManager.Instance?.CurrentRunConfig.player.characterId)?.characterSprite;
        playerSprite.sprite = SpriteManager.Instance.GetCharacterSOById(GameManager.Instance?.CurrentRunConfig.player.characterId)?.characterSprite;

        refreshEnemySprites(CombatManager.Instance?.LocalState);
    }
    
    private void refreshEnemySprites(CombatState combatState)
    {
        enemySprite.sprite = SpriteManager.Instance.GetCharacterSOById(combatState?.enemy.id)?.characterSprite;
        enemyAvatar.sprite = SpriteManager.Instance.GetCharacterSOById(combatState?.enemy.id)?.characterSprite;
    }
}
