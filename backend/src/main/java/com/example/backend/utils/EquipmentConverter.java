package com.example.backend.utils;

import com.example.backend.models.Equipment;
import jakarta.persistence.AttributeConverter;
import jakarta.persistence.Converter;
import tools.jackson.databind.ObjectMapper;

@Converter
public class EquipmentConverter implements AttributeConverter<Equipment, String> {

    private static final ObjectMapper mapper = new ObjectMapper();

    @Override
    public String convertToDatabaseColumn(Equipment equipment) {
        if (equipment == null) return null;
        try {
            return mapper.writeValueAsString(equipment);
        } catch (Exception e) {
            throw new RuntimeException("Failed to serialize Equipment", e);
        }
    }

    @Override
    public Equipment convertToEntityAttribute(String json) {
        if (json == null) return new Equipment();
        try {
            return mapper.readValue(json, Equipment.class);
        } catch (Exception e) {
            throw new RuntimeException("Failed to deserialize Equipment", e);
        }
    }
}
