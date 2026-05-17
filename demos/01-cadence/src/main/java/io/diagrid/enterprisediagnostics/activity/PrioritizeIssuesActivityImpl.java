package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.PrioritizeInput;
import io.diagrid.enterprisediagnostics.model.Priority;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import io.diagrid.enterprisediagnostics.model.Severity;
import org.springframework.stereotype.Component;

import java.util.Comparator;

@Component
public class PrioritizeIssuesActivityImpl implements PrioritizeIssuesActivity {

    @Override
    public Priority execute(PrioritizeInput input) {
        ScanResult worst = input.scans().stream()
            .min(Comparator.comparingInt(ScanResult::health))
            .orElseThrow();

        Severity severity =
            worst.health() < 50 ? Severity.RED_ALERT :
            worst.health() < 75 ? Severity.YELLOW_ALERT :
                                  Severity.GREEN;

        long belowThreshold = input.scans().stream()
            .filter(s -> s.health() < 75)
            .count();

        String summary = "%s critical at %d%% health; %d subsystem(s) below threshold"
            .formatted(worst.subsystem(), worst.health(), belowThreshold);

        return new Priority(severity, summary);
    }
}
