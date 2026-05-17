package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
public class ScanWeaponsActivityImpl implements ScanWeaponsActivity, ScanActivity {

    private static final List<String> CANDIDATES = List.of(
        "forward phaser bank 2 prefire chamber overheat",
        "torpedo tube 3 magnetic seal slow to engage",
        "EPS conduit to dorsal phaser strip resistance",
        "targeting scanner azimuth offset 0.02°",
        "photon launcher injector recoil compensation lag");

    @Override
    public ScanResult execute(ScanInput input) {
        return MockAnalysisGenerator.generate(input.subsystem(), CANDIDATES);
    }
}
