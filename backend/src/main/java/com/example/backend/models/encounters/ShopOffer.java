package com.example.backend.models.encounters;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
@JsonIgnoreProperties(ignoreUnknown = true)
public class ShopOffer {

    private String offerId;
    private OfferType type;
    private int cost;

    /** Item id — set only when type == ITEM. */
    private String itemId;

    /** Stat name ("health","attack","defense","magic") — set only when type == STAT_UPGRADE. */
    private String stat;

    /** Stat point amount — set only when type == STAT_UPGRADE. */
    private int amount;

    private boolean purchased;

    public enum OfferType {
        ITEM,
        STAT_UPGRADE
    }
}
