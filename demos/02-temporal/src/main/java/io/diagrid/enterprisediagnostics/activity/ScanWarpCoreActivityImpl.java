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
public class ScanWarpCoreActivityImpl implements ScanWarpCoreActivity {

    private static final Logger log = LoggerFactory.getLogger(ScanWarpCoreActivityImpl.class);

    private static final List<String> CANDIDATES = List.of(
        "Matter/antimatter containment field oscillating outside tolerance",
        "Dilithium crystal alignment drift detected at 0.04 microns",
        "Plasma injector #3 throughput below specification",
        "Warp field coil efficiency reduced by 6% on starboard nacelle",
        "EPS conduit overheating near main reactor manifold");

    @Override
    public ScanResult execute(ScanInput input) {
        log.info("ScanWarpCoreActivity: scanning warp core for {}", input.shipName());
        return MockAnalysisGenerator.generate(input.subsystem(), CANDIDATES);
    }
}
