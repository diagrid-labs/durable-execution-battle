using EnterpriseDiagnostics.ApiService.Models;

namespace EnterpriseDiagnostics.ApiService.Activities;

internal static class MockAnalysisGenerator
{
    public static AnalysisResult Generate(string systemName, string[] candidateIssues)
    {
        var random = Random.Shared;

        var healthPercentage = random.Next(40, 101);

        var status = healthPercentage switch
        {
            >= 90 => "Nominal",
            >= 75 => "Operational",
            >= 60 => "Degraded",
            _ => "Critical"
        };

        var maxIssues = healthPercentage switch
        {
            >= 90 => 0,
            >= 75 => 1,
            >= 60 => 2,
            _ => 3
        };

        var issueCount = maxIssues == 0 ? 0 : random.Next(1, maxIssues + 1);

        var issues = candidateIssues
            .OrderBy(_ => random.Next())
            .Take(issueCount)
            .ToArray();

        return new AnalysisResult(systemName, status, issues, healthPercentage);
    }
}
