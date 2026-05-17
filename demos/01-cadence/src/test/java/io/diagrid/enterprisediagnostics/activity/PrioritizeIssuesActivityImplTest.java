package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.PrioritizeInput;
import io.diagrid.enterprisediagnostics.model.Priority;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import io.diagrid.enterprisediagnostics.model.Severity;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.junit.jupiter.api.Assertions.*;

class PrioritizeIssuesActivityImplTest {

    private final PrioritizeIssuesActivity activity = new PrioritizeIssuesActivityImpl();

    @Test
    void redAlertWhenMinHealthBelow50() {
        Priority p = activity.execute(new PrioritizeInput("USS Enterprise", List.of(
            new ScanResult("warp_core", 45, List.of("x")),
            new ScanResult("shields",   80, List.of()))));
        assertEquals(Severity.RED_ALERT, p.severity());
        assertTrue(p.summary().startsWith("warp_core"), "summary: " + p.summary());
    }

    @Test
    void yellowAlertWhenMinHealthBelow75ButNotBelow50() {
        Priority p = activity.execute(new PrioritizeInput("USS Enterprise", List.of(
            new ScanResult("warp_core", 70, List.of("x")),
            new ScanResult("shields",   80, List.of()))));
        assertEquals(Severity.YELLOW_ALERT, p.severity());
    }

    @Test
    void greenWhenAllHealthAtOrAbove75() {
        Priority p = activity.execute(new PrioritizeInput("USS Enterprise", List.of(
            new ScanResult("warp_core", 90, List.of()),
            new ScanResult("shields",   75, List.of()))));
        assertEquals(Severity.GREEN, p.severity());
    }

    @Test
    void boundaryHealthEquals50IsYellowNotRed() {
        Priority p = activity.execute(new PrioritizeInput("USS Enterprise", List.of(
            new ScanResult("warp_core", 50, List.of()),
            new ScanResult("shields",   80, List.of()))));
        assertEquals(Severity.YELLOW_ALERT, p.severity());
    }

    @Test
    void boundaryHealthEquals75IsGreenNotYellow() {
        Priority p = activity.execute(new PrioritizeInput("USS Enterprise", List.of(
            new ScanResult("warp_core", 75, List.of()),
            new ScanResult("shields",   80, List.of()))));
        assertEquals(Severity.GREEN, p.severity());
    }

    @Test
    void summaryNamesWorstSubsystemAndCountsBelowThreshold() {
        Priority p = activity.execute(new PrioritizeInput("USS Enterprise", List.of(
            new ScanResult("warp_core", 40, List.of()),
            new ScanResult("shields",   70, List.of()),
            new ScanResult("sensors",   80, List.of()))));
        assertTrue(p.summary().contains("warp_core"));
        assertTrue(p.summary().contains("2"), "expected count=2 of subsystems <75; got: " + p.summary());
    }
}
