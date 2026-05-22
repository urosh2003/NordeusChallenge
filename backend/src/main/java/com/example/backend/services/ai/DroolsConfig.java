package com.example.backend.services.ai;

import org.drools.template.ObjectDataCompiler;
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
import java.util.List;

@Configuration
public class DroolsConfig {

    // Template data: archetype, criticalHpPct, retreatHpPct, aggressionHpPct
    private static final List<ArchetypeParams> ARCHETYPES = List.of(
            new ArchetypeParams("PHYSICAL_BRAWLER",    0.33, 0.25, 0.50),
            new ArchetypeParams("MAGIC_CASTER",        0.30, 0.30, 0.45),
            new ArchetypeParams("MAGIC_DRAINER",       0.35, 0.35, 0.50),
            new ArchetypeParams("PHYSICAL_SKIRMISHER", 0.30, 0.20, 0.55),
            new ArchetypeParams("BALANCED_TANK",       0.25, 0.15, 0.35)
    );

    private static final String[] STATIC_DRLS = {
            "drools/level1-perception.drl",
            "drools/accumulate-burst.drl",
            "drools/cep-patterns.drl",
            "drools/backward-queries.drl",
            "drools/level2-tactics.drl",
            "drools/level3-action.drl",
            "drools/fallback.drl",
    };

    private static final String ARCHETYPE_TEMPLATE = "drools/archetype-rules.drt";

    @Bean
    public KieBase kieBase() throws IOException {
        KieServices ks = KieServices.Factory.get();
        KieFileSystem kfs = ks.newKieFileSystem();

        // Load static DRL files from classpath
        for (String classpath : STATIC_DRLS) {
            String content = loadClasspathText(classpath);
            kfs.write("src/main/resources/" + classpath, content);
        }

        // Generate archetype-specific rules from the .drt template using ObjectDataCompiler
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

    // Drools template engine compiles archetype-rules.drt against the ARCHETYPES list,
    // producing one EvaluateHealthCritical_<X> + ChooseAggressiveTactic_<X> pair per row.
    private String generateArchetypeDrl() throws IOException {
        try (InputStream template = getClass().getClassLoader().getResourceAsStream(ARCHETYPE_TEMPLATE)) {
            if (template == null) throw new IOException("Template not found: " + ARCHETYPE_TEMPLATE);
            return new ObjectDataCompiler().compile(ARCHETYPES, template);
        }
    }
}
