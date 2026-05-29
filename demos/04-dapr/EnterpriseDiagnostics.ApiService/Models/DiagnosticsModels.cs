namespace EnterpriseDiagnostics.ApiService.Models;

public record DiagnosticsInput(
    string Id,
    string ShipName,
    string DiagnosticsDate,
    string EngineerName,
    bool Testing = false);

public record DiagnosticsOutput(
    string ShipName,
    string DiagnosticsDate,
    string EngineerName,
    Priority Priority,
    string? BridgeNotification);

public record ScanInput(string ShipName, string Subsystem);

public record ScanResult(string Subsystem, int Health, string[] Anomalies);

public record PrioritizeInput(string ShipName, ScanResult[] Scans);

public record Priority(string Severity, string Summary);

public record BridgeNotificationInput(
    string ShipName,
    string EngineerName,
    Priority Priority);

public record BridgeNotificationResult(string Message);
