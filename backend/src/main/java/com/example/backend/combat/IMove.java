package com.example.backend.combat;

import lombok.Getter;

import java.util.List;

@Getter
public abstract class IMove {
    private final String moveId;

    public IMove(String moveId) {
        this.moveId = moveId;
    }

    public abstract boolean canExecute(MoveContext ctx);
    public abstract List<CombatEvent> execute(MoveContext ctx);
}
