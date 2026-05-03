package com.example.backend.services.items;

import com.example.backend.models.items.ItemDefinition;
import org.springframework.core.io.Resource;
import org.springframework.core.io.ResourceLoader;
import org.springframework.stereotype.Component;
import tools.jackson.databind.ObjectMapper;

import java.io.IOException;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

@Component
public class ItemRegistry {

    private final Map<String, ItemDefinition> items;

    public ItemRegistry(ObjectMapper objectMapper, ResourceLoader resourceLoader) throws IOException {
        Resource resource = resourceLoader.getResource("classpath:items.json");
        List<ItemDefinition> defs = objectMapper.readValue(
                resource.getInputStream(),
                objectMapper.getTypeFactory().constructCollectionType(List.class, ItemDefinition.class)
        );
        this.items = defs.stream().collect(Collectors.toMap(ItemDefinition::getId, d -> d));
    }

    public ItemDefinition getItem(String id) {
        ItemDefinition def = items.get(id);
        if (def == null) throw new IllegalArgumentException("Unknown item: " + id);
        return def;
    }

    public boolean exists(String id) {
        return items.containsKey(id);
    }

    public Map<String, ItemDefinition> getAll() {
        return items;
    }
}
