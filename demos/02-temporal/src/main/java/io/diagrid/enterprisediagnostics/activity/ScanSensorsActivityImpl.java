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
public class ScanSensorsActivityImpl implements ScanSensorsActivity {

    private static final Logger log = LoggerFactory.getLogger(ScanSensorsActivityImpl.class);

    private static final List<String> CANDIDATES = List.of(
        "Lateral sensor array gain calibration drift detected",
        "Subspace bandwidth allocation below recommended threshold",
        "Long-range sensor pallet 3 reporting elevated noise floor",
        "Navigational deflector return signal showing phase jitter",
        "Tachyon detection grid misaligned by 0.12 degrees");

    @Override
    public ScanResult execute(ScanInput input) {
        log.info("ScanSensorsActivity: scanning sensors for {}", input.shipName());
        return MockAnalysisGenerator.generate(input.subsystem(), CANDIDATES);
    }
}
