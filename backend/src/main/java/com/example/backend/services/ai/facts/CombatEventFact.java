package com.example.backend.services.ai.facts;

import com.example.backend.services.ai.MoveCategory;
import lombok.Data;
import lombok.NoArgsConstructor;
import org.kie.api.definition.type.Role;
import org.kie.api.definition.type.Timestamp;

@Role(Role.Type.EVENT)
@Timestamp("turnTimestamp")
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
