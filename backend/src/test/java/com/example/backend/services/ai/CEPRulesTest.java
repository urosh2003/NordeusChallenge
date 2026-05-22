package com.example.backend.services.ai;

import com.example.backend.services.ai.facts.BurstDamageAssessment;
import com.example.backend.services.ai.facts.CantFinishPlayer;
import com.example.backend.services.ai.facts.CombatEventFact;
import com.example.backend.services.ai.facts.EnemyFact;
import com.example.backend.services.ai.facts.PlayerBehaviorProfile;
import com.example.backend.services.ai.facts.PlayerFact;
import org.junit.jupiter.api.Test;
import org.kie.api.KieBase;
import org.kie.api.KieServices;
import org.kie.api.runtime.ClassObjectFilter;
import org.kie.api.runtime.KieSession;
import org.kie.api.runtime.KieSessionConfiguration;
import org.kie.api.runtime.conf.ClockTypeOption;
import org.kie.api.time.SessionPseudoClock;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;

import java.util.Collection;
import java.util.concurrent.TimeUnit;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

@SpringBootTest
class CEPRulesTest {

    @Autowired
    private KieBase kieBase;

    private KieSession newSession() {
        KieSessionConfiguration cfg = KieServices.Factory.get().newKieSessionConfiguration();
        cfg.setOption(ClockTypeOption.get("pseudo"));
        return kieBase.newKieSession(cfg, null);
    }

    // Seeds the working memory with the minimum facts required for CEP rules to fire:
    // CantFinishPlayer (gate marker) + EnemyFact (provides maxHp for burst threshold) + PlayerFact.
    private void seedBaseFacts(KieSession session, int enemyMaxHp) {
        session.insert(new CantFinishPlayer());

        EnemyFact ef = new EnemyFact();
        ef.setId("enemy");
        ef.setArchetype(Archetype.BALANCED_TANK);
        ef.setMaxHp(enemyMaxHp);
        ef.setHpPercent(1.0);
        ef.setMaxMana(20);
        ef.setCurrentMana(20);
        ef.setMaxStamina(20);
        ef.setCurrentStamina(20);
        ef.setAttack(5);
        ef.setDefense(5);
        ef.setMagic(5);
        session.insert(ef);

        PlayerFact pf = new PlayerFact();
        pf.setCurrentHp(100);
        pf.setMaxHp(100);
        pf.setAttack(5);
        pf.setDefense(5);
        pf.setMagic(5);
        session.insert(pf);
    }

    private void insertDamageEvent(KieSession session, String source, int amount) {
        SessionPseudoClock clock = session.getSessionClock();
        clock.advanceTime(1, TimeUnit.MILLISECONDS);

        CombatEventFact e = new CombatEventFact();
        e.setType("DAMAGE_DEALT");
        e.setSource(source);
        e.setAmount(amount);
        e.setTurnTimestamp(clock.getCurrentTime());

        session.getEntryPoint("combat-stream").insert(e);
    }

    private void insertMoveUsedEvent(KieSession session, String source, MoveCategory category) {
        SessionPseudoClock clock = session.getSessionClock();
        clock.advanceTime(1, TimeUnit.MILLISECONDS);

        CombatEventFact e = new CombatEventFact();
        e.setType("MOVE_USED");
        e.setSource(source);
        e.setMoveCategory(category);
        e.setTurnTimestamp(clock.getCurrentTime());

        session.getEntryPoint("combat-stream").insert(e);
    }

    private <T> Collection<T> factsOf(KieSession session, Class<T> clazz) {
        @SuppressWarnings("unchecked")
        Collection<T> objects = (Collection<T>) session.getObjects(new ClassObjectFilter(clazz));
        return objects;
    }

    private boolean hasBehaviorProfile(KieSession session, BehaviorType type) {
        return factsOf(session, PlayerBehaviorProfile.class).stream()
                .anyMatch(p -> p.getType() == type);
    }

    // ── AssessBurstDamageRisk ────────────────────────────────────────────────

    @Test
    void burstDamage_detected_whenSumExceeds35PercentMaxHp() {
        KieSession session = newSession();
        try {
            seedBaseFacts(session, 100);   // threshold = 35
            insertDamageEvent(session, "player", 12);
            insertDamageEvent(session, "player", 14);
            insertDamageEvent(session, "player", 11);   // sum 37 > 35
            session.fireAllRules();

            Collection<BurstDamageAssessment> bda = factsOf(session, BurstDamageAssessment.class);
            assertEquals(1, bda.size(), "Expected exactly one BurstDamageAssessment");
            BurstDamageAssessment fact = bda.iterator().next();
            assertEquals(Severity.HIGH, fact.getSeverity());
            assertEquals(37, fact.getTotal());
        } finally {
            session.dispose();
        }
    }

    @Test
    void burstDamage_notDetected_whenSumBelowThreshold() {
        KieSession session = newSession();
        try {
            seedBaseFacts(session, 100);
            insertDamageEvent(session, "player", 5);
            insertDamageEvent(session, "player", 5);
            insertDamageEvent(session, "player", 5);    // sum 15 < 35
            session.fireAllRules();

            assertEquals(0, factsOf(session, BurstDamageAssessment.class).size());
        } finally {
            session.dispose();
        }
    }

    @Test
    void burstDamage_notDetected_whenSourceIsEnemy() {
        KieSession session = newSession();
        try {
            seedBaseFacts(session, 100);
            insertDamageEvent(session, "enemy", 50);
            insertDamageEvent(session, "enemy", 50);
            insertDamageEvent(session, "enemy", 50);
            session.fireAllRules();

            assertEquals(0, factsOf(session, BurstDamageAssessment.class).size(),
                    "Burst rule should ignore damage sourced from enemy");
        } finally {
            session.dispose();
        }
    }

    // ── DetectPlayerPhysicalSpammer ──────────────────────────────────────────

    @Test
    void physicalSpammer_detected_when3PhysicalIn4MoveEvents() {
        KieSession session = newSession();
        try {
            seedBaseFacts(session, 100);
            insertMoveUsedEvent(session, "player", MoveCategory.DAMAGE_PHYSICAL);
            insertMoveUsedEvent(session, "player", MoveCategory.DAMAGE_PHYSICAL);
            insertMoveUsedEvent(session, "player", MoveCategory.SELF_BUFF);
            insertMoveUsedEvent(session, "player", MoveCategory.DAMAGE_PHYSICAL);
            session.fireAllRules();

            assertTrue(hasBehaviorProfile(session, BehaviorType.PHYSICAL_SPAMMER));
        } finally {
            session.dispose();
        }
    }

    @Test
    void physicalSpammer_notDetected_whenOnly2PhysicalIn4MoveEvents() {
        KieSession session = newSession();
        try {
            seedBaseFacts(session, 100);
            insertMoveUsedEvent(session, "player", MoveCategory.DAMAGE_PHYSICAL);
            insertMoveUsedEvent(session, "player", MoveCategory.SELF_BUFF);
            insertMoveUsedEvent(session, "player", MoveCategory.SELF_BUFF);
            insertMoveUsedEvent(session, "player", MoveCategory.DAMAGE_PHYSICAL);
            session.fireAllRules();

            assertFalse(hasBehaviorProfile(session, BehaviorType.PHYSICAL_SPAMMER));
        } finally {
            session.dispose();
        }
    }

    // ── DetectPlayerComboSetup ───────────────────────────────────────────────

    @Test
    void comboPlayer_detected_whenDebuffFollowedByPhysicalAttack() {
        KieSession session = newSession();
        try {
            seedBaseFacts(session, 100);
            insertMoveUsedEvent(session, "player", MoveCategory.ENEMY_DEBUFF);
            insertMoveUsedEvent(session, "player", MoveCategory.DAMAGE_PHYSICAL);
            session.fireAllRules();

            assertTrue(hasBehaviorProfile(session, BehaviorType.COMBO_PLAYER));
        } finally {
            session.dispose();
        }
    }

    @Test
    void comboPlayer_detected_whenDebuffFollowedByMagicAttack() {
        KieSession session = newSession();
        try {
            seedBaseFacts(session, 100);
            insertMoveUsedEvent(session, "player", MoveCategory.ENEMY_DEBUFF);
            insertMoveUsedEvent(session, "player", MoveCategory.DAMAGE_MAGIC);
            session.fireAllRules();

            assertTrue(hasBehaviorProfile(session, BehaviorType.COMBO_PLAYER));
        } finally {
            session.dispose();
        }
    }

    @Test
    void comboPlayer_notDetected_withoutFollowUpAttack() {
        KieSession session = newSession();
        try {
            seedBaseFacts(session, 100);
            insertMoveUsedEvent(session, "player", MoveCategory.ENEMY_DEBUFF);
            insertMoveUsedEvent(session, "player", MoveCategory.SELF_BUFF);
            session.fireAllRules();

            assertFalse(hasBehaviorProfile(session, BehaviorType.COMBO_PLAYER));
        } finally {
            session.dispose();
        }
    }
}
