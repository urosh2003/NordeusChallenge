package com.example.backend.repositories;

import com.example.backend.models.CombatInstance;
import com.example.backend.models.CombatStatus;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.Optional;
import java.util.UUID;

@Repository
public interface CombatInstanceRepository extends JpaRepository<CombatInstance, UUID> {
    boolean existsByRunIdAndStatus(UUID runId, CombatStatus status);
    boolean existsByStatus(CombatStatus status);
    Optional<CombatInstance> findByRunIdAndStatus(UUID runId, CombatStatus status);
}
