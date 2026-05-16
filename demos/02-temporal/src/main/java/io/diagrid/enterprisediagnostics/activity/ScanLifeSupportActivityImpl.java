package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import io.temporal.spring.boot.ActivityImpl;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
@ActivityImpl(taskQueues = "diagnostics-tq")
public class ScanLifeSupportActivityImpl implements ScanLifeSupportActivity {

    private static final Logger log = LoggerFactory.getLogger(ScanLifeSupportActivityImpl.class);

    private static final List<String> CANDIDATES = List.of(
        "Atmospheric CO2 scrubber efficiency reduced by 4.7% on deck 9",
        "Environmental control humidity regulators showing intermittent faults in crew quarters",
        "Oxygen recycler intake filter contamination detected in main air processor",
        "Thermal regulation imbalance reported across deck 12 corridor sections",
        "Gravity plating fluctuation outside nominal range in cargo bay 4");

    @Override
    public ScanResult execute(ScanInput input) {
        log.info("ScanLifeSupportActivity: scanning life support for {}", input.shipName());
        return MockAnalysisGenerator.generate(input.subsystem(), CANDIDATES);
    }
}
