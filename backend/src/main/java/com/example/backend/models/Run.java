package com.example.backend.models;

import com.example.backend.utils.EncounterSlotListConverter;
import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

@Entity
@Table(name = "runs")
@Getter
@Setter
@NoArgsConstructor
public class Run {

    @Id
    @GeneratedValue(strategy = GenerationType.UUID)
    private UUID id;

    @Enumerated(EnumType.STRING)
    private RunStatus status = RunStatus.ACTIVE;

    private int currentEncounterIndex = 0;

    @Convert(converter = EncounterSlotListConverter.class)
    @Column(columnDefinition = "text")
    private List<EncounterSlot> encounters = new ArrayList<>();
}
