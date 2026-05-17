package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
public class ScanWarpCoreActivityImpl implements ScanWarpCoreActivity, ScanActivity {

    private static final List<String> CANDIDATES = List.of(
        "plasma conduit micro-fracture (deck 36)",
        "warp coil phase variance 0.04%",
        "dilithium articulation frame torque drift",
        "matter/antimatter injector hesitation",
        "magnetic constriction segment 7 thermal creep");

    @Override
    public ScanResult execute(ScanInput input) {
        return MockAnalysisGenerator.generate(input.subsystem(), CANDIDATES);
    }
}
