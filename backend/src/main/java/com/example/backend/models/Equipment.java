package com.example.backend.models;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import lombok.Data;

import java.util.List;
import java.util.stream.Collectors;
import java.util.stream.Stream;

@Data
@JsonIgnoreProperties(ignoreUnknown = true)
public class Equipment {
    private String mainHand;
    private String offHand;
    private String armor;
    private String gloves;
    private String shoes;
    private String amulet;
    private String ring;

    public List<String> getAllEquipped() {
        return Stream.of(mainHand, offHand, armor, gloves, shoes, amulet, ring)
                .filter(id -> id != null && !id.isEmpty())
                .collect(Collectors.toList());
    }
}
