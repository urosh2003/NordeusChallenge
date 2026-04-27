package com.example.backend.repositories;

import com.example.backend.models.Run;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.UUID;

public interface RunRepository extends JpaRepository<Run, UUID> {}
