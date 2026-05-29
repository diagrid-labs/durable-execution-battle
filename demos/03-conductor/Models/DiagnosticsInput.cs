namespace EnterpriseDiagnostics.Conductor.Models;

public record DiagnosticsInput(
    string Id,
    string ShipName,
    string DiagnosticsDate,
    string EngineerName,
    bool Testing = false);
