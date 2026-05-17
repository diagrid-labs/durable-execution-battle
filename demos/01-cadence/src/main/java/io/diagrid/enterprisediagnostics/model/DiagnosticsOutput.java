package io.diagrid.enterprisediagnostics.model;

public record DiagnosticsOutput(
    String shipName,
    String diagnosticsDate,
    String engineerName,
    Priority priority,
    BridgeNotificationResult bridgeNotification) {}
