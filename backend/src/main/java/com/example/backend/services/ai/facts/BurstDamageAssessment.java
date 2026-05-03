package com.example.backend.services.ai.facts;

import com.example.backend.services.ai.Severity;
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
