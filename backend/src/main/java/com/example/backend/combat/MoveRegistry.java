package com.example.backend.combat;

import com.example.backend.exceptions.InvalidMoveException;
import org.springframework.stereotype.Component;

import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

@Component
public class MoveRegistry {

    private final Map<String, IMove> moves;

    public MoveRegistry(List<IMove> moves) {
        this.moves = moves.stream().collect(Collectors.toMap(IMove::getId, m -> m));
    }

    public IMove get(String moveId) {
        IMove move = moves.get(moveId);
        if (move == null) {
            throw new InvalidMoveException("Unknown move: " + moveId);
        }
        return move;
    }
}
