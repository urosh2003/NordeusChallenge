package com.example.backend.ai.facts;

import com.example.backend.ai.MoveCategory;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
public class CombatEventFact {
    private String type;
    private String source;
    private String target;
    private MoveCategory moveCategory;
    private int amount;
    private long turnTimestamp;
}
