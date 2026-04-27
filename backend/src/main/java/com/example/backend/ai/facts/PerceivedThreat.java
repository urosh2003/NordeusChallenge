package com.example.backend.ai.facts;

import com.example.backend.ai.ThreatLevel;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class PerceivedThreat {
    private ThreatLevel level;
}
