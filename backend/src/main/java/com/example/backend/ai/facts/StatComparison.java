package com.example.backend.ai.facts;

import com.example.backend.ai.StatComparisonType;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class StatComparison {
    private StatComparisonType type;
}
