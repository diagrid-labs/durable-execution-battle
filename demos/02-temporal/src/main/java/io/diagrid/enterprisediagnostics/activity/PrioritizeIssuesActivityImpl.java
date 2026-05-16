package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.PrioritizeInput;
import io.diagrid.enterprisediagnostics.model.Priority;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import io.diagrid.enterprisediagnostics.model.Severity;
import io.temporal.spring.boot.ActivityImpl;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

import java.util.Comparator;

@Component
@ActivityImpl(taskQueues = "diagnostics-tq")
public class PrioritizeIssuesActivityImpl implements PrioritizeIssuesActivity {

    private static final Logger log = LoggerFactory.getLogger(PrioritizeIssuesActivityImpl.class);

    @Override
    public Priority execute(PrioritizeInput input) {
        log.info("PrioritizeIssuesActivity: prioritizing for {}", input.shipName());

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

        String summary = "Worst subsystem: %s @ %d%% health. %d of %d subsystems below operational threshold."
            .formatted(worst.subsystem(), worst.health(), belowThreshold, input.scans().size());

        return new Priority(severity, summary);
    }
}
