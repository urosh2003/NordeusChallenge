using System;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    AudioSource audioSource;
    [SerializeField] AudioClip damageSound;
    [SerializeField] AudioClip healSound;
    [SerializeField] AudioClip statusSound;
    [SerializeField] AudioClip lvlUpSound;
    [SerializeField] AudioClip environmentSound;
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        CombatEventProcessor.OnAnyEvent += PlayMoveSound;
    }
    private void OnDestroy()
    {
        CombatEventProcessor.OnAnyEvent -= PlayMoveSound;
    }

    private void PlayMoveSound(CombatEvent e)
    {
        if (e.type == nameof(CombatEventType.DAMAGE_DEALT))
        {
            audioSource.PlayOneShot(damageSound, 1);
        }
        else if (e.type == nameof(CombatEventType.HEAL_RECEIVED))
        {
            audioSource.PlayOneShot(healSound, 1);
        }
        else if (e.type == nameof(CombatEventType.STATUS_EFFECT_APPLIED) ||
                 e.type == nameof(CombatEventType.STATUS_EFFECT_EXPIRED))
        {
            audioSource.PlayOneShot(statusSound, 1);
        }
        else if (e.type == nameof(CombatEventType.ENVIRONMENT_EFFECT))
        {
            audioSource.PlayOneShot(environmentSound, 1);
        }
        else if (e.type == nameof(CombatEventType.LEVEL_UP))
        {
            audioSource.PlayOneShot(lvlUpSound, 1);
        }
    }
}
