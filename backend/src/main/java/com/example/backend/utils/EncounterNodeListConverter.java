package com.example.backend.utils;

import com.example.backend.models.encounters.EncounterNode;
import jakarta.persistence.AttributeConverter;
import jakarta.persistence.Converter;
import tools.jackson.databind.ObjectMapper;

import java.util.ArrayList;
import java.util.List;

@Converter
public class EncounterNodeListConverter implements AttributeConverter<List<EncounterNode>, String> {

    private static final ObjectMapper mapper = new ObjectMapper();

    @Override
    public String convertToDatabaseColumn(List<EncounterNode> list) {
        if (list == null) return null;
        try {
            return mapper.writeValueAsString(list);
        } catch (Exception e) {
            throw new RuntimeException("Failed to serialize encounter nodes", e);
        }
    }

    @Override
    public List<EncounterNode> convertToEntityAttribute(String json) {
        if (json == null) return new ArrayList<>();
        try {
            return mapper.readValue(json,
                    mapper.getTypeFactory().constructCollectionType(List.class, EncounterNode.class));
        } catch (Exception e) {
            throw new RuntimeException("Failed to deserialize encounter nodes", e);
        }
    }
}
