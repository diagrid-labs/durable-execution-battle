using Dapr.Workflow;
using EnterpriseDiagnostics.ApiService.Models;

namespace EnterpriseDiagnostics.ApiService.Activities;

internal sealed partial class AnalyzeWarpCoreActivity(
    ILogger<AnalyzeWarpCoreActivity> logger) : WorkflowActivity<AnalysisInput, AnalysisResult>
{
    private static readonly string[] CandidateIssues =
    [
        "Matter/antimatter containment field oscillating outside tolerance",
        "Dilithium crystal alignment drift detected at 0.04 microns",
        "Plasma injector #3 throughput below specification",
        "Warp field coil efficiency reduced by 6% on starboard nacelle",
        "EPS conduit overheating near main reactor manifold"
    ];

    public override Task<AnalysisResult> RunAsync(
        WorkflowActivityContext context,
        AnalysisInput input)
    {
        LogActivity(logger, input.ShipName);

        // Just to simulate some processing, and you have time to pause the workflow.
        Thread.Sleep(2500);

        var result = MockAnalysisGenerator.Generate("Warp Core", CandidateIssues);
        return Task.FromResult(result);
    }

    [LoggerMessage(LogLevel.Information, "AnalyzeWarpCoreActivity: Analyzing warp core for {ShipName}")]
    static partial void LogActivity(ILogger logger, string ShipName);
}
