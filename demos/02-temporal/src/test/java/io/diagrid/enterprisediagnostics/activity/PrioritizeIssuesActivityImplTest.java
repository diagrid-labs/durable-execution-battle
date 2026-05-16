package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.PrioritizeInput;
import io.diagrid.enterprisediagnostics.model.Priority;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import io.diagrid.enterprisediagnostics.model.Severity;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;

class PrioritizeIssuesActivityImplTest {

    private final PrioritizeIssuesActivity activity = new PrioritizeIssuesActivityImpl();

    @Test
    void minHealthBelow50YieldsRedAlert() {
        Priority p = activity.execute(new PrioritizeInput("ship", List.of(
            new ScanResult("warp_core",    40, List.of("x")),
            new ScanResult("shields",      80, List.of("y")),
            new ScanResult("life_support", 90, List.of("z")))));
        assertThat(p.severity()).isEqualTo(Severity.RED_ALERT);
        assertThat(p.summary()).contains("warp_core").contains("40");
    }

    @Test
    void minHealthBetween50And74YieldsYellowAlert() {
        Priority p = activity.execute(new PrioritizeInput("ship", List.of(
            new ScanResult("warp_core", 60, List.of("x")),
            new ScanResult("shields",   90, List.of("y")))));
        assertThat(p.severity()).isEqualTo(Severity.YELLOW_ALERT);
    }

    @Test
    void minHealthAtOrAbove75YieldsGreen() {
        Priority p = activity.execute(new PrioritizeInput("ship", List.of(
            new ScanResult("warp_core", 80, List.of("x")),
            new ScanResult("shields",   95, List.of("y")))));
        assertThat(p.severity()).isEqualTo(Severity.GREEN);
    }

    @Test
    void summaryReportsBelowThresholdCount() {
        Priority p = activity.execute(new PrioritizeInput("ship", List.of(
            new ScanResult("warp_core",    40, List.of("x")),
            new ScanResult("shields",      60, List.of("y")),
            new ScanResult("life_support", 90, List.of("z")))));
        // 2 of 3 below 75
        assertThat(p.summary()).contains("2");
    }
}
