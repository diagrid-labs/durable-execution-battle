package io.diagrid.enterprisediagnostics.model;

import java.util.List;

public record ScanResult(String subsystem, int health, List<String> anomalies) {}
