using EnterpriseDiagnostics.ApiService.Models;

namespace EnterpriseDiagnostics.ApiService.Activities;

internal static class MockAnalysisGenerator
{
    public static ScanResult Generate(string subsystem, string[] candidateAnomalies)
    {
        var random = Random.Shared;

        // Biased low so the aggregate severity reliably lands on red_alert for the demo.
        var health = random.Next(30, 66);

        var maxAnomalies = health switch
        {
            >= 60 => 1,
            >= 45 => 2,
            _     => 3
        };

        var anomalyCount = random.Next(1, maxAnomalies + 1);

        var anomalies = candidateAnomalies
            .OrderBy(_ => random.Next())
            .Take(anomalyCount)
            .ToArray();

        return new ScanResult(subsystem, health, anomalies);
    }
}
