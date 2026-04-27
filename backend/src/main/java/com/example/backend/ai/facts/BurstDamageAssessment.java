package com.example.backend.ai.facts;

import com.example.backend.ai.Severity;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class BurstDamageAssessment {
    private int total;
    private Severity severity;
}
