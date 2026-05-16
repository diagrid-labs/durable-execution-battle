using Dapr.Workflow;
using EnterpriseDiagnostics.ApiService.Models;

namespace EnterpriseDiagnostics.ApiService.Activities;

internal sealed partial class AnalyzeHullActivity(
    ILogger<AnalyzeHullActivity> logger) : WorkflowActivity<AnalysisInput, AnalysisResult>
{
    private static readonly string[] CandidateIssues =
    [
        "Micro-fractures detected on dorsal hull plating, section 7-G",
        "Structural integrity field fluctuating at 3.2% above baseline",
        "Radiation shielding degraded in shuttle bay 2",
        "Hull plating ablation exceeding nominal levels near port nacelle",
        "Tritanium lattice stress detected at deck 14 junction"
    ];

    public override Task<AnalysisResult> RunAsync(
        WorkflowActivityContext context,
        AnalysisInput input)
    {
        LogActivity(logger, input.ShipName);

        // Just to simulate some processing, and you have time to pause the workflow.
        Thread.Sleep(2500);

        var result = MockAnalysisGenerator.Generate("Hull", CandidateIssues);
        return Task.FromResult(result);
    }

    [LoggerMessage(LogLevel.Information, "AnalyzeHullActivity: Analyzing hull for {ShipName}")]
    static partial void LogActivity(ILogger logger, string ShipName);
}
