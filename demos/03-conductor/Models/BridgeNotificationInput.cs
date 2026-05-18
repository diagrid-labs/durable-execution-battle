namespace EnterpriseDiagnostics.Conductor.Models;

public record BridgeNotificationInput(
    string ShipName,
    string EngineerName,
    Priority Priority);
