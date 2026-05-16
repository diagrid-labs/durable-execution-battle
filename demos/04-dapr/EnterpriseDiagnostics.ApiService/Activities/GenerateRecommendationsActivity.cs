using Dapr.Workflow;
using EnterpriseDiagnostics.ApiService.Models;

namespace EnterpriseDiagnostics.ApiService.Activities;

internal sealed partial class GenerateRecommendationsActivity(
    ILogger<GenerateRecommendationsActivity> logger) : WorkflowActivity<RecommendationsInput, RecommendationsResult>
{
    public override Task<RecommendationsResult> RunAsync(
        WorkflowActivityContext context,
        RecommendationsInput input)
    {
        LogActivity(logger, input.ShipName);

        // Just to simulate some processing, and you have time to pause the workflow.
        Thread.Sleep(2500);

        var analyses = new[]
        {
            input.HullAnalysis,
            input.WarpCoreAnalysis,
            input.SecurityProtocolsAnalysis,
            input.WeaponSystemsAnalysis,
            input.LifeSupportSystemsAnalysis
        }
        .Where(a => a is not null)
        .Select(a => a!)
        .ToArray();

        var nonNominal = analyses
            .Where(a => !string.Equals(a.Status, "Nominal", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var recommendations = nonNominal.Length == 0
            ? new[] { "All systems nominal - no maintenance recommended." }
            : nonNominal
                .Select(a =>
                {
                    var firstIssue = a.Issues.Length > 0 ? a.Issues[0] : "general inspection";
                    return $"Schedule maintenance for {a.SystemName} ({a.Status}, {a.HealthPercentage}% health): {firstIssue}";
                })
                .ToArray();

        var priorities = analyses
            .OrderBy(a => a.HealthPercentage)
            .Select((a, index) => $"P{index + 1}: {a.SystemName} - {a.Status} ({a.HealthPercentage}%)")
            .ToArray();

        return Task.FromResult(new RecommendationsResult(recommendations, priorities));
    }

    [LoggerMessage(LogLevel.Information, "GenerateRecommendationsActivity: Generating recommendations for {ShipName}")]
    static partial void LogActivity(ILogger logger, string ShipName);
}
