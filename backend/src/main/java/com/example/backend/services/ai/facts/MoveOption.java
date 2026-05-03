package com.example.backend.services.ai.facts;

import com.example.backend.services.ai.MoveCategory;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
public class MoveOption {
    private String moveId;
    private MoveCategory moveCategory;
    private String costType;
    private int costValue;
    private int projectedValue;
}
