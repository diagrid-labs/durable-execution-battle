package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
public class ScanSensorsActivityImpl implements ScanSensorsActivity, ScanActivity {

    private static final List<String> CANDIDATES = List.of(
        "lateral sensor array calibration drift",
        "subspace gradient detector noise floor +3dB",
        "long-range sensor pallet 2 packet loss",
        "navigational deflector ghost return at 14 AU",
        "tachyon detection grid intermittent");

    @Override
    public ScanResult execute(ScanInput input) {
        return MockAnalysisGenerator.generate(input.subsystem(), CANDIDATES);
    }
}
