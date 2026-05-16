package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanResult;
import org.junit.jupiter.api.RepeatedTest;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;

class MockAnalysisGeneratorTest {

    private static final List<String> CANDIDATES = List.of("a", "b", "c", "d", "e");

    @Test
    void resultEchoesSubsystemId() {
        ScanResult r = MockAnalysisGenerator.generate("warp_core", CANDIDATES);
        assertThat(r.subsystem()).isEqualTo("warp_core");
    }

    @RepeatedTest(100)
    void healthIsBetween30And65Inclusive() {
        ScanResult r = MockAnalysisGenerator.generate("warp_core", CANDIDATES);
        assertThat(r.health()).isBetween(30, 65);
    }

    @RepeatedTest(100)
    void anomaliesAreNonEmptyAndUniqueAndSubsetOfCandidates() {
        ScanResult r = MockAnalysisGenerator.generate("warp_core", CANDIDATES);
        assertThat(r.anomalies()).isNotEmpty();
        assertThat(r.anomalies()).doesNotHaveDuplicates();
        assertThat(CANDIDATES).containsAll(r.anomalies());
    }

    @RepeatedTest(50)
    void anomalyCountCapsAtThree() {
        ScanResult r = MockAnalysisGenerator.generate("warp_core", CANDIDATES);
        assertThat(r.anomalies().size()).isBetween(1, 3);
    }
}
