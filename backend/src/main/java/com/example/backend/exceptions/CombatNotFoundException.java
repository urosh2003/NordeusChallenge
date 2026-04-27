package com.example.backend.exceptions;

import java.util.UUID;

public class CombatNotFoundException extends RuntimeException {
    public CombatNotFoundException(UUID id) {
        super("Combat not found: " + id);
    }
}
