package com.example.backend.services.combats;

import com.example.backend.models.characters.Character;
import com.example.backend.models.combats.CombatEvent;
import com.example.backend.models.combats.CombatState;
import com.example.backend.exceptions.InvalidMoveException;
import com.example.backend.models.moves.IMove;
import com.example.backend.models.moves.MoveContext;
import com.example.backend.services.moves.MoveRegistry;
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
