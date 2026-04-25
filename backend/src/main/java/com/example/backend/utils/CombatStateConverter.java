package com.example.backend.utils;

import com.example.backend.models.CombatState;
import tools.jackson.databind.ObjectMapper;
import jakarta.persistence.AttributeConverter;
import jakarta.persistence.Converter;

@Converter
public class CombatStateConverter implements AttributeConverter<CombatState, String> {

    private static final ObjectMapper mapper = new ObjectMapper();

    @Override
    public String convertToDatabaseColumn(CombatState state) {
        if (state == null) return null;
        try {
            return mapper.writeValueAsString(state);
        } catch (Exception e) {
            throw new RuntimeException("Failed to serialize CombatState", e);
        }
    }

    @Override
    public CombatState convertToEntityAttribute(String json) {
        if (json == null) return null;
        try {
            return mapper.readValue(json, CombatState.class);
        } catch (Exception e) {
            throw new RuntimeException("Failed to deserialize CombatState", e);
        }
    }
}
