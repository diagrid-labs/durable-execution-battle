package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanResult;
import org.junit.jupiter.api.RepeatedTest;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.junit.jupiter.api.Assertions.*;

class MockAnalysisGeneratorTest {

    private static final List<String> CANDIDATES = List.of("a", "b", "c", "d", "e");

    @RepeatedTest(50)
    void healthIsAlwaysBetween30And65Inclusive() {
        ScanResult r = MockAnalysisGenerator.generate("warp_core", CANDIDATES);
        assertTrue(r.health() >= 30, "health was " + r.health());
        assertTrue(r.health() <= 65, "health was " + r.health());
    }

    @RepeatedTest(50)
    void returnsBetween1And3AnomaliesInclusive() {
        ScanResult r = MockAnalysisGenerator.generate("shields", CANDIDATES);
        assertTrue(r.anomalies().size() >= 1, "got " + r.anomalies().size());
        assertTrue(r.anomalies().size() <= 3, "got " + r.anomalies().size());
    }

    @RepeatedTest(50)
    void anomaliesAreDrawnFromCandidates() {
        ScanResult r = MockAnalysisGenerator.generate("sensors", CANDIDATES);
        for (String a : r.anomalies()) {
            assertTrue(CANDIDATES.contains(a), "unexpected anomaly: " + a);
        }
    }

    @Test
    void subsystemIsReturnedVerbatim() {
        ScanResult r = MockAnalysisGenerator.generate("life_support", CANDIDATES);
        assertEquals("life_support", r.subsystem());
    }

    @RepeatedTest(50)
    void anomaliesAreUnique() {
        ScanResult r = MockAnalysisGenerator.generate("weapons", CANDIDATES);
        assertEquals(r.anomalies().size(), r.anomalies().stream().distinct().count(),
            "duplicates in anomalies list");
    }
}
