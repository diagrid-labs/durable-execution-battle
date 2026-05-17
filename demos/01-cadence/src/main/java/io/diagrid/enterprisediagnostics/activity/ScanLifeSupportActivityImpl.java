package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
public class ScanLifeSupportActivityImpl implements ScanLifeSupportActivity, ScanActivity {

    private static final List<String> CANDIDATES = List.of(
        "CO2 scrubber efficiency 87% on deck 12",
        "atmospheric processor inlet contamination",
        "gravity plating fluctuation crew quarters 4F",
        "thermal regulation cold-spot deck 22",
        "emergency oxygen reserve seal #3 micro-leak");

    @Override
    public ScanResult execute(ScanInput input) {
        return MockAnalysisGenerator.generate(input.subsystem(), CANDIDATES);
    }
}
