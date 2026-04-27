package com.example.backend.ai;

import org.kie.api.KieBase;
import org.kie.api.KieBaseConfiguration;
import org.kie.api.KieServices;
import org.kie.api.builder.KieBuilder;
import org.kie.api.builder.KieFileSystem;
import org.kie.api.builder.Message;
import org.kie.api.conf.EventProcessingOption;
import org.kie.api.runtime.KieContainer;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import java.io.IOException;
import java.io.InputStream;
import java.nio.charset.StandardCharsets;

@Configuration
public class DroolsConfig {

    // Template data: archetype, criticalHpPct, retreatHpPct, aggressionHpPct
    private static final ArchetypeParams[] ARCHETYPES = {
            new ArchetypeParams("PHYSICAL_BRAWLER",    0.33, 0.25, 0.50),
            new ArchetypeParams("MAGIC_CASTER",        0.30, 0.30, 0.45),
            new ArchetypeParams("MAGIC_DRAINER",       0.35, 0.35, 0.50),
            new ArchetypeParams("PHYSICAL_SKIRMISHER", 0.30, 0.20, 0.55),
            new ArchetypeParams("BALANCED_TANK",       0.25, 0.15, 0.35),
    };

    private static final String[] STATIC_DRLS = {
            "drools/level1-perception.drl",
            "drools/accumulate-burst.drl",
            "drools/cep-patterns.drl",
            "drools/backward-vulnerability.drl",
            "drools/level3-action.drl",
            "drools/fallback.drl",
    };

    @Bean
    public KieBase kieBase() throws IOException {
        KieServices ks = KieServices.Factory.get();
        KieFileSystem kfs = ks.newKieFileSystem();

        // Load static DRL files from classpath
        for (String classpath : STATIC_DRLS) {
            String content = loadClasspathText(classpath);
            kfs.write("src/main/resources/" + classpath, content);
        }

        // Template-generated DRL (same rule structure per archetype, different threshold values)
        kfs.write("src/main/resources/drools/template-archetypes.drl", generateArchetypeDrl());

        String kmoduleXml = """
                <?xml version="1.0" encoding="UTF-8"?>
                <kmodule xmlns="http://www.drools.org/xsd/kmodule">
                    <kbase name="CombatAI" eventProcessingMode="stream"
                           packages="com.example.backend.ai.rules">
                        <ksession name="CombatAISession" type="stateful" clockType="pseudo"/>
                    </kbase>
                </kmodule>
                """;
        kfs.writeKModuleXML(kmoduleXml);

        KieBuilder builder = ks.newKieBuilder(kfs).buildAll();
        if (builder.getResults().hasMessages(Message.Level.ERROR)) {
            throw new IllegalStateException(
                    "Drools build errors:\n" + builder.getResults().getMessages());
        }

        KieContainer container = ks.newKieContainer(builder.getKieModule().getReleaseId());

        KieBaseConfiguration kbConfig = ks.newKieBaseConfiguration();
        kbConfig.setOption(EventProcessingOption.STREAM);
        return container.newKieBase("CombatAI", kbConfig);
    }

    private String loadClasspathText(String path) throws IOException {
        try (InputStream is = getClass().getClassLoader().getResourceAsStream(path)) {
            if (is == null) throw new IOException("Classpath resource not found: " + path);
            return new String(is.readAllBytes(), StandardCharsets.UTF_8);
        }
    }

    /**
     * Template: generates one L1 HP-perception rule and one L2 aggression rule per archetype.
     * The threshold values (criticalHpPct, aggressionHpPct) vary per archetype row.
     * All five rows share the exact same rule structure — this is what Drools templates do.
     */
    private String generateArchetypeDrl() {
        StringBuilder drl = new StringBuilder();
        drl.append("package com.example.backend.ai.rules\n\n");
        drl.append("import com.example.backend.ai.facts.EnemyFact;\n");
        drl.append("import com.example.backend.ai.facts.PerceivedThreat;\n");
        drl.append("import com.example.backend.ai.facts.Tactic;\n");
        drl.append("import com.example.backend.ai.Archetype;\n");
        drl.append("import com.example.backend.ai.ThreatLevel;\n");
        drl.append("import com.example.backend.ai.TacticType;\n\n");

        for (ArchetypeParams a : ARCHETYPES) {
            // L1: HP perception — archetype-specific critical threshold
            drl.append(String.format("""
                    rule "EvaluateHealthCritical_%s"
                        salience 100
                    when
                        EnemyFact(archetype == Archetype.%s, hpPercent < %s)
                        not PerceivedThreat()
                    then
                        insert(new PerceivedThreat(ThreatLevel.CRITICAL));
                    end

                    """, a.name(), a.name(), a.criticalHpPct()));

            // L2: Aggressive tactic — archetype-specific aggression threshold
            drl.append(String.format("""
                    rule "ChooseAggressiveTactic_%s"
                        salience 50
                    when
                        EnemyFact(archetype == Archetype.%s, hpPercent >= %s)
                        not PerceivedThreat(level == ThreatLevel.CRITICAL)
                        not Tactic()
                    then
                        insert(new Tactic(TacticType.MAXIMIZE_DAMAGE));
                    end

                    """, a.name(), a.name(), a.aggressionHpPct()));
        }

        return drl.toString();
    }
}
