namespace EnterpriseDiagnostics.Conductor.Models;

public record DiagnosticsOutput(
    string ShipName,
    string DiagnosticsDate,
    string EngineerName,
    Priority Priority,
    string? BridgeNotification);
