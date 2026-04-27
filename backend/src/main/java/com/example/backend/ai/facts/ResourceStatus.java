package com.example.backend.ai.facts;

import com.example.backend.ai.ResourceStatusType;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class ResourceStatus {
    private ResourceStatusType type;
    private String resourceName;
}
