package com.example.backend.services;

import com.example.backend.combat.CombatEvent;
import com.example.backend.combat.IMove;
import com.example.backend.combat.MoveContext;
import com.example.backend.combat.MoveRegistry;
import com.example.backend.models.Character;
import com.example.backend.models.CombatState;
import com.example.backend.exceptions.InvalidMoveException;
import org.springframework.stereotype.Service;

import java.util.List;

@Service
public class CombatService {

    private final MoveRegistry moveRegistry;

    public CombatService(MoveRegistry moveRegistry) {
        this.moveRegistry = moveRegistry;
    }

    public List<CombatEvent> executeMove(CombatState state, Character actor, Character target, String moveId) {
        IMove move = moveRegistry.get(moveId);
        MoveContext ctx = new MoveContext(state, actor, target);
        if (!move.canExecute(ctx)) {
            throw new InvalidMoveException("Cannot execute move: " + moveId);
        }
        return move.execute(ctx);
    }
}
