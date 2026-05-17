package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanResult;

import java.security.SecureRandom;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

public final class MockAnalysisGenerator {

    private static final SecureRandom RANDOM = new SecureRandom();

    private MockAnalysisGenerator() {}

    public static ScanResult generate(String subsystem, List<String> candidateAnomalies) {
        int health = RANDOM.nextInt(30, 66);
        int maxAnomalies = (health >= 60) ? 1 : (health >= 45) ? 2 : 3;
        int anomalyCount = RANDOM.nextInt(1, maxAnomalies + 1);

        List<String> shuffled = new ArrayList<>(candidateAnomalies);
        Collections.shuffle(shuffled, RANDOM);
        List<String> picked = shuffled.subList(0, anomalyCount);

        return new ScanResult(subsystem, health, List.copyOf(picked));
    }
}
