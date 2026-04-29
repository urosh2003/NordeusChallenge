package com.example.backend.services;

import com.example.backend.models.Character;
import com.example.backend.models.CharacterDefinition;
import com.example.backend.models.CharacterStats;
import org.springframework.core.io.Resource;
import org.springframework.core.io.ResourceLoader;
import org.springframework.stereotype.Component;
import tools.jackson.databind.ObjectMapper;

import java.io.IOException;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

@Component
public class CharacterLoader {

    private final Map<String, CharacterDefinition> definitions;

    public CharacterLoader(ObjectMapper objectMapper, ResourceLoader resourceLoader) throws IOException {
        Resource resource = resourceLoader.getResource("classpath:characters.json");
        List<CharacterDefinition> defs = objectMapper.readValue(
                resource.getInputStream(),
                objectMapper.getTypeFactory().constructCollectionType(List.class, CharacterDefinition.class)
        );
        this.definitions = defs.stream()
                .collect(Collectors.toMap(CharacterDefinition::getId, d -> d));
    }

    public CharacterDefinition getDefinition(String definitionId) {
        CharacterDefinition def = definitions.get(definitionId);
        if (def == null) throw new IllegalArgumentException("Unknown character definition: " + definitionId);
        return def;
    }

    public List<CharacterDefinition> getStartingClasses() {
        return definitions.values().stream()
                .filter(CharacterDefinition::isStartingClass)
                .collect(Collectors.toList());
    }

    public Character createCharacter(String instanceId, String definitionId, int level) {
        CharacterDefinition def = definitions.get(definitionId);
        if (def == null) throw new IllegalArgumentException("Unknown character definition: " + definitionId);
        CharacterStats stats = computeStats(def, level);
        return new Character(
                instanceId,
                def.getName(),
                stats.getHealth()  * 10,
                stats.getMagic()   * 5,
                stats.getAttack()  * 5,
                stats,
                def.getMoves(),
                new ArrayList<>()
        );
    }

    private CharacterStats computeStats(CharacterDefinition def, int level) {
        CharacterStats base = def.getBaseStats();
        CharacterStats gains = def.getStatsPerLevel();
        int lvl = level - 1;
        return new CharacterStats(
                base.getHealth()  + lvl * gains.getHealth(),
                base.getAttack()  + lvl * gains.getAttack(),
                base.getDefense() + lvl * gains.getDefense(),
                base.getMagic()   + lvl * gains.getMagic()
        );
    }
}
