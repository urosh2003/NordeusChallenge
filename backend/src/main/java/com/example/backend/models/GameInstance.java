package com.example.backend.models;

import com.example.backend.utils.CombatStateConverter;
import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.util.UUID;

@Entity
@Table(name = "games")
@Getter
@Setter
@NoArgsConstructor
public class GameInstance {

    @Id
    @GeneratedValue(strategy = GenerationType.UUID)
    private UUID id;

    @Enumerated(EnumType.STRING)
    private GameStatus status;

    @Enumerated(EnumType.STRING)
    private Turn currentTurn;

    @Version
    private Long version;

    @Convert(converter = CombatStateConverter.class)
    @Column(columnDefinition = "text")
    private CombatState state;
}
