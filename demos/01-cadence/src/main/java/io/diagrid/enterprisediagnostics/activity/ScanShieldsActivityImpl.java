package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
public class ScanShieldsActivityImpl implements ScanShieldsActivity, ScanActivity {

    private static final List<String> CANDIDATES = List.of(
        "shield emitter 4 power flicker",
        "ablative armor delamination forward dorsal",
        "deflector phase polarization drift 0.7°",
        "graviton field harmonic instability",
        "structural integrity field starboard nacelle pylon");

    @Override
    public ScanResult execute(ScanInput input) {
        return MockAnalysisGenerator.generate(input.subsystem(), CANDIDATES);
    }
}
