package com.example.backend.repositories;

import com.example.backend.models.players.PlayerState;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;
import java.util.UUID;

public interface PlayerStateRepository extends JpaRepository<PlayerState, UUID> {
    Optional<PlayerState> findFirstByOrderByIdAsc();
    Optional<PlayerState> findByRunId(UUID runId);
}
