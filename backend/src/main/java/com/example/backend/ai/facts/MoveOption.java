package com.example.backend.ai.facts;

import com.example.backend.ai.MoveCategory;
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
