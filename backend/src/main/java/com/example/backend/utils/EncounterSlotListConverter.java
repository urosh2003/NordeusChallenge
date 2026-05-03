package com.example.backend.utils;

import com.example.backend.models.encounters.EncounterSlot;
import jakarta.persistence.AttributeConverter;
import jakarta.persistence.Converter;
import tools.jackson.databind.ObjectMapper;

import java.util.ArrayList;
import java.util.List;

@Converter
public class EncounterSlotListConverter implements AttributeConverter<List<EncounterSlot>, String> {

    private static final ObjectMapper mapper = new ObjectMapper();

    @Override
    public String convertToDatabaseColumn(List<EncounterSlot> list) {
        if (list == null) return null;
        try {
            return mapper.writeValueAsString(list);
        } catch (Exception e) {
            throw new RuntimeException("Failed to serialize encounters", e);
        }
    }

    @Override
    public List<EncounterSlot> convertToEntityAttribute(String json) {
        if (json == null) return new ArrayList<>();
        try {
            return mapper.readValue(json,
                    mapper.getTypeFactory().constructCollectionType(List.class, EncounterSlot.class));
        } catch (Exception e) {
            throw new RuntimeException("Failed to deserialize encounters", e);
        }
    }
}
