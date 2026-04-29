package com.example.backend.dtos.responces;

import com.example.backend.combat.CombatEvent;
import com.example.backend.models.CombatState;

import java.util.List;
import java.util.UUID;

public record CombatResponse(UUID combatId, List<CombatEvent> events, CombatState state, PlayerStateResponse playerState, String currentTurn, String environmentId) {}
