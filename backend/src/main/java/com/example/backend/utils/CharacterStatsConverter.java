package com.example.backend.utils;

import com.example.backend.models.characters.CharacterStats;
import jakarta.persistence.AttributeConverter;
import jakarta.persistence.Converter;
import tools.jackson.databind.ObjectMapper;

@Converter
public class CharacterStatsConverter implements AttributeConverter<CharacterStats, String> {

    private static final ObjectMapper mapper = new ObjectMapper();

    @Override
    public String convertToDatabaseColumn(CharacterStats stats) {
        if (stats == null) return null;
        try {
            return mapper.writeValueAsString(stats);
        } catch (Exception e) {
            throw new RuntimeException("Failed to serialize CharacterStats", e);
        }
    }

    @Override
    public CharacterStats convertToEntityAttribute(String json) {
        if (json == null) return null;
        try {
            return mapper.readValue(json, CharacterStats.class);
        } catch (Exception e) {
            throw new RuntimeException("Failed to deserialize CharacterStats", e);
        }
    }
}
