namespace EnterpriseDiagnostics.Conductor.Models;

public record ScanResult(string Subsystem, int Health, string[] Anomalies);
