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
public class ScanShieldsActivityImpl implements ScanShieldsActivity {

    private static final Logger log = LoggerFactory.getLogger(ScanShieldsActivityImpl.class);

    private static final List<String> CANDIDATES = List.of(
        "Deflector grid phase variance exceeding nominal range",
        "Shield generator 2 recharge rate slower than baseline",
        "Forward shield emitter coupling showing micro-arc events",
        "Harmonic resonance detected in dorsal shield projector",
        "EPS tap to aft shield array running 8% under spec");

    @Override
    public ScanResult execute(ScanInput input) {
        log.info("ScanShieldsActivity: scanning shields for {}", input.shipName());
        return MockAnalysisGenerator.generate(input.subsystem(), CANDIDATES);
    }
}
