package io.diagrid.enterprisediagnostics.model;

public record DiagnosticsInput(
    String id,
    String shipName,
    String diagnosticsDate,
    String engineerName) {}
