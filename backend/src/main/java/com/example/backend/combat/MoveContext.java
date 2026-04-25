package com.example.backend.combat;

import com.example.backend.models.Character;
import com.example.backend.models.CombatState;

public record MoveContext(CombatState state, Character actor, Character target) {

}
