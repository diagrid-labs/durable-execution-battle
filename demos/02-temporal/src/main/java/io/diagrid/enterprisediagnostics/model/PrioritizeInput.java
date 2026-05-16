package io.diagrid.enterprisediagnostics.model;

import java.util.List;

public record PrioritizeInput(String shipName, List<ScanResult> scans) {}
