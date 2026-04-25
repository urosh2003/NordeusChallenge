package com.example.backend.dtos.responces;

import com.example.backend.combat.GameEvent;
import com.example.backend.models.CombatState;

import java.util.List;
import java.util.UUID;

public record GameResponse(UUID gameId, List<GameEvent> events, CombatState state) {}
