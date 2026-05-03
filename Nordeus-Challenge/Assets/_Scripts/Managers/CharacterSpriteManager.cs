using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSpriteManager : MonoBehaviour
{
    [SerializeField] private SpriteRenderer playerSprite;
    [SerializeField] private SpriteRenderer enemySprite;
    [SerializeField] private Image playerAvatar;
    [SerializeField] private Image enemyAvatar;
    [SerializeField] private float spriteFadeTime = 0.2f;

    void Start()
    {
        CombatManager.OnCombatInitialized += RefreshEnemySprites;
        CombatEventProcessor.OnAnyEvent += HitSprite;
        
        playerAvatar.sprite = SpriteManager.Instance.GetCharacterSOById(GameManager.Instance?.CurrentRunConfig.player.characterId)?.characterSprite;
        playerSprite.sprite = SpriteManager.Instance.GetCharacterSOById(GameManager.Instance?.CurrentRunConfig.player.characterId)?.characterSprite;

        RefreshEnemySprites(CombatManager.Instance?.LocalState);
    }

    void OnDestroy()
    {
        CombatManager.OnCombatInitialized -= RefreshEnemySprites;
        CombatEventProcessor.OnAnyEvent -= HitSprite;
    }

    private void HitSprite(CombatEvent e)
    {
        if (e == null) return;

        bool isPlayerTarget = (e.targetId == "player");


        SpriteRenderer targetSprite = isPlayerTarget
            ? playerSprite
            : enemySprite;

        if (e.type == nameof(CombatEventType.DAMAGE_DEALT))
        {
            StartCoroutine(FlashColor(targetSprite, Color.red));
        }
        else if (e.type == nameof(CombatEventType.HEAL_RECEIVED) || e.type == nameof(CombatEventType.RESOURCE_REGEN) )
        {
            StartCoroutine(FlashColor(targetSprite, Color.green));
        }
        else if (e.type == nameof(CombatEventType.STATUS_EFFECT_APPLIED) || e.type == nameof(CombatEventType.STATUS_EFFECT_EXPIRED) || e.type == nameof(CombatEventType.ENVIRONMENT_EFFECT))
        {
            StartCoroutine(FlashColor(targetSprite, Color.yellow));
        }
        else if (e.type == nameof(CombatEventType.LEVEL_UP) || e.type == nameof(CombatEventType.XP_GAINED))
        {
            StartCoroutine(FlashColor(targetSprite, Color.blue));
        }
        else if (e.type == nameof(CombatEventType.COMBAT_ENDED))
        {
            StartCoroutine(FlashColor(enemySprite, Color.clear));
        }
    }
    
    private IEnumerator FlashColor(SpriteRenderer sprite, Color flashColor)
    {
        if (sprite == null) yield break;

        Color originalColor = sprite.color;

        float t = 0f;

        while (t < spriteFadeTime)
        {
            t += Time.deltaTime;
            sprite.color = Color.Lerp(originalColor, flashColor, t / spriteFadeTime);
            yield return null;
        }

        if  (flashColor == Color.clear) yield break;
        t = 0f;

        while (t < spriteFadeTime)
        {
            t += Time.deltaTime;
            sprite.color = Color.Lerp(flashColor, originalColor, t / spriteFadeTime);
            yield return null;
        }

        sprite.color = originalColor;
    }
    
    private void RefreshEnemySprites(CombatState combatState)
    {
        enemySprite.color = Color.white;
        enemySprite.sprite = SpriteManager.Instance.GetCharacterSOById(combatState?.enemy.id)?.characterSprite;
        enemyAvatar.sprite = SpriteManager.Instance.GetCharacterSOById(combatState?.enemy.id)?.characterSprite;
    }
    
}
