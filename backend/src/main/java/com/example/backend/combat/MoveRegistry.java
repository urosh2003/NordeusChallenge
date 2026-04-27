package com.example.backend.combat;

import com.example.backend.combat.moves.DataDrivenMove;
import com.example.backend.exceptions.InvalidMoveException;
import org.springframework.core.io.Resource;
import org.springframework.core.io.ResourceLoader;
import org.springframework.stereotype.Component;
import tools.jackson.databind.ObjectMapper;

import java.io.IOException;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

@Component
public class MoveRegistry {

    private final Map<String, IMove> moves;
    private final Map<String, MoveDefinition> definitions;

    public MoveRegistry(ObjectMapper objectMapper, ResourceLoader resourceLoader) throws IOException {
        Resource resource = resourceLoader.getResource("classpath:moves.json");
        List<MoveDefinition> defs = objectMapper.readValue(
                resource.getInputStream(),
                objectMapper.getTypeFactory().constructCollectionType(List.class, MoveDefinition.class)
        );
        this.definitions = defs.stream()
                .collect(Collectors.toMap(MoveDefinition::getId, d -> d));
        this.moves = defs.stream()
                .collect(Collectors.toMap(MoveDefinition::getId, DataDrivenMove::new));
    }

    public IMove get(String moveId) {
        IMove move = moves.get(moveId);
        if (move == null) throw new InvalidMoveException("Unknown move: " + moveId);
        return move;
    }

    public MoveDefinition getDefinition(String moveId) {
        MoveDefinition def = definitions.get(moveId);
        if (def == null) throw new InvalidMoveException("Unknown move: " + moveId);
        return def;
    }
}
