namespace EnterpriseDiagnostics.Conductor.Models;

public record PrioritizeInput(string ShipName, ScanResult[] Scans);
