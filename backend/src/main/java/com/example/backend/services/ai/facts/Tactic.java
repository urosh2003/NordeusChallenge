package com.example.backend.services.ai.facts;

import com.example.backend.services.ai.TacticType;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class Tactic {
    private TacticType type;
}
