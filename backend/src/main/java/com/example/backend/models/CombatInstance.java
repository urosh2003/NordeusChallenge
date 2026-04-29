package com.example.backend.models;

import com.example.backend.utils.CombatStateConverter;
import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.util.UUID;

@Entity
@Table(name = "combats")
@Getter
@Setter
@NoArgsConstructor
public class CombatInstance {

    @Id
    @GeneratedValue(strategy = GenerationType.UUID)
    private UUID id;

    @Enumerated(EnumType.STRING)
    private CombatStatus status;

    @Enumerated(EnumType.STRING)
    private Turn currentTurn;

    @Version
    private Long version;

    @Convert(converter = CombatStateConverter.class)
    @Column(columnDefinition = "text")
    private CombatState state;

    private int enemyLevel = 1;
    private String enemyDefinitionId;

    // Set when this combat belongs to a run
    private UUID runId;
    private String encounterNodeId;
    private String environmentId;
}
