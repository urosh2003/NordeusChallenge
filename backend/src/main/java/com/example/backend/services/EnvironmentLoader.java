package com.example.backend.services;

import com.example.backend.models.EnvironmentDefinition;
import org.springframework.core.io.Resource;
import org.springframework.core.io.ResourceLoader;
import org.springframework.stereotype.Component;
import tools.jackson.databind.ObjectMapper;

import java.io.IOException;
import java.util.Collection;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

@Component
public class EnvironmentLoader {

    private final Map<String, EnvironmentDefinition> definitions;

    public EnvironmentLoader(ObjectMapper objectMapper, ResourceLoader resourceLoader) throws IOException {
        Resource resource = resourceLoader.getResource("classpath:environments.json");
        List<EnvironmentDefinition> defs = objectMapper.readValue(
                resource.getInputStream(),
                objectMapper.getTypeFactory().constructCollectionType(List.class, EnvironmentDefinition.class)
        );
        this.definitions = defs.stream()
                .collect(Collectors.toMap(EnvironmentDefinition::getId, d -> d));
    }

    public EnvironmentDefinition getDefinition(String environmentId) {
        EnvironmentDefinition def = definitions.get(environmentId);
        if (def == null) throw new IllegalArgumentException("Unknown environment: " + environmentId);
        return def;
    }

    public Collection<EnvironmentDefinition> getAll() {
        return definitions.values();
    }
}
