package com.example.backend.ai.facts;

import com.example.backend.ai.BehaviorType;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class PlayerBehaviorProfile {
    private BehaviorType type;
}
