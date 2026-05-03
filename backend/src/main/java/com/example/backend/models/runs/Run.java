package com.example.backend.models.runs;

import com.example.backend.models.encounters.EncounterNode;
import com.example.backend.utils.EncounterNodeListConverter;
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

    private String currentNodeId;

    @Convert(converter = EncounterNodeListConverter.class)
    @Column(columnDefinition = "text")
    private List<EncounterNode> nodes = new ArrayList<>();
}
