using Dapr.Workflow;
using EnterpriseDiagnostics.ApiService.Models;

namespace EnterpriseDiagnostics.ApiService.Activities;

internal sealed partial class AnalyzeLifeSupportSystemsActivity(
    ILogger<AnalyzeLifeSupportSystemsActivity> logger) : WorkflowActivity<AnalysisInput, AnalysisResult>
{
    private static readonly string[] CandidateIssues =
    [
        "Atmospheric CO2 scrubber efficiency reduced by 4.7% on deck 9",
        "Environmental control humidity regulators showing intermittent faults in crew quarters",
        "Oxygen recycler intake filter contamination detected in main air processor",
        "Thermal regulation imbalance reported across deck 12 corridor sections",
        "Gravity plating fluctuation outside nominal range in cargo bay 4"
    ];

    public override Task<AnalysisResult> RunAsync(
        WorkflowActivityContext context,
        AnalysisInput input)
    {
        LogActivity(logger, input.ShipName);

        // Just to simulate some processing, and you have time to pause the workflow.
        Thread.Sleep(2500);

        var result = MockAnalysisGenerator.Generate("Life Support Systems", CandidateIssues);
        return Task.FromResult(result);
    }

    [LoggerMessage(LogLevel.Information, "AnalyzeLifeSupportSystemsActivity: Analyzing life support systems for {ShipName}")]
    static partial void LogActivity(ILogger logger, string ShipName);
}
