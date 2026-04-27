package com.example.backend.dtos.requests;

public record StatDistributionRequest(int health, int attack, int defense, int magic) {
    public int total() {
        return health + attack + defense + magic;
    }
}
