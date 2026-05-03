package com.example.backend.models.moves;

import com.example.backend.models.characters.Character;
import com.example.backend.models.combats.CombatState;

public record MoveContext(CombatState state, Character actor, Character target) {

}
