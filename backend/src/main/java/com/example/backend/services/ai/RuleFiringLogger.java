package com.example.backend.services.ai;

import org.kie.api.event.rule.AfterMatchFiredEvent;
import org.kie.api.event.rule.DefaultAgendaEventListener;
import org.kie.api.event.rule.DefaultRuleRuntimeEventListener;
import org.kie.api.event.rule.ObjectDeletedEvent;
import org.kie.api.event.rule.ObjectInsertedEvent;
import org.kie.api.event.rule.ObjectUpdatedEvent;
import org.kie.api.runtime.KieSession;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

// Attaches Drools event listeners that route rule activations and working-memory
// changes through SLF4J so they appear alongside the rest of the Spring log output.
// Configure verbosity via application.properties:
//     logging.level.AI.Drools=INFO   (default: see all rules + inserts)
//     logging.level.AI.Drools=WARN   (silence the per-turn trace)
public final class RuleFiringLogger {

    private static final Logger log = LoggerFactory.getLogger("AI.Drools");

    private RuleFiringLogger() {}

    public static void attach(KieSession session) {
        session.addEventListener(new DefaultAgendaEventListener() {
            @Override
            public void afterMatchFired(AfterMatchFiredEvent event) {
                log.info("FIRED  {}", event.getMatch().getRule().getName());
            }
        });

        session.addEventListener(new DefaultRuleRuntimeEventListener() {
            @Override
            public void objectInserted(ObjectInsertedEvent event) {
                log.info("INSERT {}", event.getObject());
            }

            @Override
            public void objectUpdated(ObjectUpdatedEvent event) {
                log.info("UPDATE {}", event.getObject());
            }

            @Override
            public void objectDeleted(ObjectDeletedEvent event) {
                log.info("DELETE {}", event.getOldObject());
            }
        });
    }
}
