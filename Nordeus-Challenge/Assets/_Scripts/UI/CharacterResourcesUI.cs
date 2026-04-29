using System;
using TMPro;
using UnityEngine;

public class CharacterResourcesUI : MonoBehaviour
{
        [SerializeField] TextMeshProUGUI hpText;
        [SerializeField] TextMeshProUGUI manaText;
        [SerializeField] TextMeshProUGUI staminaText;
        public TrackingCharacter trackingCharacter = TrackingCharacter.PLAYER;

        private void OnEnable()
        {
                if (trackingCharacter == TrackingCharacter.PLAYER)
                        CombatManager.OnPlayerChanged += UpdateUI;
                else
                        CombatManager.OnEnemyChanged += UpdateUI;

                // Sync immediately in case the init event already fired before we subscribed.
                var state = CombatManager.Instance?.LocalState;
                if (state != null)
                        UpdateUI(trackingCharacter == TrackingCharacter.PLAYER ? state.player : state.enemy);
        }

        private void OnDisable()
        {
                if (trackingCharacter == TrackingCharacter.PLAYER)
                        CombatManager.OnPlayerChanged -= UpdateUI;
                else
                        CombatManager.OnEnemyChanged -= UpdateUI;
        }

        private void UpdateUI(Character character)
        {
                if (character == null) return;
                hpText.text      = $"Health: {character.currentHp}/{character.maxHp}";
                manaText.text    = $"Mana: {character.currentMana}/{character.maxMana}";
                staminaText.text = $"Stamina: {character.currentStamina}/{character.maxStamina}";
        }
}
