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
public class ScanWeaponsActivityImpl implements ScanWeaponsActivity {

    private static final Logger log = LoggerFactory.getLogger(ScanWeaponsActivityImpl.class);

    private static final List<String> CANDIDATES = List.of(
        "Phaser array bank 3 power coupling shows intermittent drop",
        "Photon torpedo launcher loading mechanism response time elevated",
        "Targeting sensor calibration drift detected on aft array",
        "Tactical computer subroutine queue depth exceeding threshold",
        "Quantum torpedo magazine humidity outside recommended range");

    @Override
    public ScanResult execute(ScanInput input) {
        log.info("ScanWeaponsActivity: scanning weapons for {}", input.shipName());
        return MockAnalysisGenerator.generate(input.subsystem(), CANDIDATES);
    }
}
