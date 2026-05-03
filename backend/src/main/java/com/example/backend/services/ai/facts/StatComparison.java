package com.example.backend.services.ai.facts;

import com.example.backend.services.ai.StatComparisonType;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class StatComparison {
    private StatComparisonType type;
}
