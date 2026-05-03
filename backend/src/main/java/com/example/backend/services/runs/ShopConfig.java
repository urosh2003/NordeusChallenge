package com.example.backend.services.runs;

import lombok.Data;
import org.springframework.stereotype.Component;
import tools.jackson.databind.ObjectMapper;

import java.io.IOException;
import java.io.InputStream;
import java.util.Map;

@Component
public class ShopConfig {

    private final ConfigData data;

    public ShopConfig() {
        try (InputStream is = getClass().getClassLoader().getResourceAsStream("shop-config.json")) {
            if (is == null) throw new IllegalStateException("shop-config.json not found on classpath");
            this.data = new ObjectMapper().readValue(is, ConfigData.class);
        } catch (IOException e) {
            throw new IllegalStateException("Failed to load shop-config.json", e);
        }
    }

    public int getStatOfferCount()                         { return data.statOfferCount; }
    public int getItemOfferCount()                         { return data.itemOfferCount; }
    public int getStatAmountMin()                          { return data.statAmountMin; }
    public int getStatAmountMax()                          { return data.statAmountMax; }
    public int getStatCostPerPoint()                       { return data.statCostPerPoint; }
    public int getSellPricePercent()                       { return data.sellPricePercent; }
    public Map<String, Integer> getEncounterTypeWeights()  { return data.encounterTypeWeights; }

    @Data
    public static class ConfigData {
        private int statOfferCount;
        private int itemOfferCount;
        private int statAmountMin;
        private int statAmountMax;
        private int statCostPerPoint;
        private int sellPricePercent;
        private Map<String, Integer> encounterTypeWeights;
    }
}
