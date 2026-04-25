package com.example.backend.combat;

import java.util.List;

public interface IMove {
    String getId();
    boolean canExecute(MoveContext ctx);
    List<GameEvent> execute(MoveContext ctx);
}
