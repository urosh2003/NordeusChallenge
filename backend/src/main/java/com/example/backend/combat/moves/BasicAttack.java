package com.example.backend.combat.moves;

import com.example.backend.combat.GameEventType;
import com.example.backend.combat.MoveContext;
import com.example.backend.combat.GameEvent;
import com.example.backend.combat.IMove;
import com.example.backend.models.Character;
import org.springframework.stereotype.Component;

import java.util.ArrayList;
import java.util.List;

@Component
public class BasicAttack implements IMove {

    @Override
    public String getId() { return "basic_attack"; }

    @Override
    public boolean canExecute(MoveContext ctx) { return true; }

    @Override
    public List<GameEvent> execute(MoveContext ctx) {
        Character actor = ctx.actor();
        Character target = ctx.target();
        List<GameEvent> events = new ArrayList<>();

        events.add(GameEvent.of(GameEventType.MOVE_USED)
                .with("actorId", actor.getId())
                .with("moveId", getId())
                .with("targetId", target.getId()));

        int damage = Math.max(1, actor.getStats().getAttack() - target.getStats().getDefense());
        target.setCurrentHp(target.getCurrentHp() - damage);

        events.add(GameEvent.of(GameEventType.DAMAGE_DEALT)
                .with("actorId", target.getId())
                .with("amount", damage)
                .with("sourceMoveId", getId()));

        return events;
    }
}
