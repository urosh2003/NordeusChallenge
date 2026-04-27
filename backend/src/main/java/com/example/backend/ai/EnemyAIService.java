package com.example.backend.ai;

import com.example.backend.models.Character;
import com.example.backend.models.CombatState;
import org.springframework.stereotype.Service;

@Service
public class EnemyAIService {

    private final DroolsEnemyAIService drools;

    public EnemyAIService(DroolsEnemyAIService drools) {
        this.drools = drools;
    }

    public String pickMove(CombatState state, Character enemy, Character player,
                           String enemyDefinitionId) {
        return drools.pickMove(state, enemy, player, enemyDefinitionId);
    }
}
