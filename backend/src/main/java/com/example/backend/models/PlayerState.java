package com.example.backend.models;

import com.example.backend.utils.CharacterStatsConverter;
import com.example.backend.utils.EquipmentConverter;
import com.example.backend.utils.StringListConverter;
import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

@Entity
@Table(name = "player_state")
@Getter
@Setter
@NoArgsConstructor
public class PlayerState {

    @Id
    @GeneratedValue(strategy = GenerationType.UUID)
    private UUID id;

    private String characterDefinitionId;
    private String characterName;

    private int level = 1;
    private int currentXp = 0;

    private int pendingStatPoints = 0;

    @Convert(converter = CharacterStatsConverter.class)
    @Column(columnDefinition = "text")
    private CharacterStats stats;

    @Convert(converter = StringListConverter.class)
    @Column(columnDefinition = "text")
    private List<String> knownMoves = new ArrayList<>();

    @Convert(converter = StringListConverter.class)
    @Column(columnDefinition = "text")
    private List<String> equippedMoves = new ArrayList<>();

    // All items the player owns but has not equipped
    @Convert(converter = StringListConverter.class)
    @Column(columnDefinition = "text")
    private List<String> inventory = new ArrayList<>();

    // Currently equipped items (one per slot)
    @Convert(converter = EquipmentConverter.class)
    @Column(columnDefinition = "text")
    private Equipment equipment = new Equipment();

    public int getXpToNextLevel() {
        return 1 << (level - 1);
    }
}
