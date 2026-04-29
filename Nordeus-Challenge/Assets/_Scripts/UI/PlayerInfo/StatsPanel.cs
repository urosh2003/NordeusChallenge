using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rebuilds four stat rows (Health, Attack, Defense, Magic) showing base value
/// and equipment bonus. Recalculates against the current *pending* equipment so
/// the breakdown updates live as the player drags items around.
///
/// Prefab requirements for statRowPrefab:
///   StatRowUI component, see StatRowUI for its own prefab requirements.
/// </summary>
public class StatsPanel : MonoBehaviour
{
    [SerializeField] Transform  container;
    [SerializeField] GameObject statRowPrefab;

    private readonly List<GameObject> _rows = new();

    public void Refresh(CharacterStats baseStats, Equipment pendingEquip, RunConfig config)
    {
        foreach (var r in _rows) Destroy(r);
        _rows.Clear();

        int bHp = 0, bAtk = 0, bDef = 0, bMag = 0;
        foreach (var id in pendingEquip.GetAllEquipped())
        {
            var bonus = config?.GetItem(id)?.bonusStats;
            if (bonus == null) continue;
            bHp  += bonus.health;
            bAtk += bonus.attack;
            bDef += bonus.defense;
            bMag += bonus.magic;
        }

        SpawnRow("Health",  baseStats.health,  bHp);
        SpawnRow("Attack",  baseStats.attack,  bAtk);
        SpawnRow("Defense", baseStats.defense, bDef);
        SpawnRow("Magic",   baseStats.magic,   bMag);
    }

    private void SpawnRow(string statName, int baseVal, int bonus)
    {
        var go = Instantiate(statRowPrefab, container);
        go.GetComponent<StatRowUI>()?.Setup(statName, baseVal, bonus);
        _rows.Add(go);
    }
}
