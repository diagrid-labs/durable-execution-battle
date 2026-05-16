using Dapr.Workflow;
using EnterpriseDiagnostics.ApiService.Models;

namespace EnterpriseDiagnostics.ApiService.Activities;

internal sealed partial class ScanWarpCoreActivity(
    ILogger<ScanWarpCoreActivity> logger) : WorkflowActivity<ScanInput, ScanResult>
{
    private static readonly string[] CandidateAnomalies =
    [
        "Matter/antimatter containment field oscillating outside tolerance",
        "Dilithium crystal alignment drift detected at 0.04 microns",
        "Plasma injector #3 throughput below specification",
        "Warp field coil efficiency reduced by 6% on starboard nacelle",
        "EPS conduit overheating near main reactor manifold"
    ];

    public override Task<ScanResult> RunAsync(
        WorkflowActivityContext context,
        ScanInput input)
    {
        LogActivity(logger, input.ShipName);
        return Task.FromResult(MockAnalysisGenerator.Generate(input.Subsystem, CandidateAnomalies));
    }

    [LoggerMessage(LogLevel.Information, "ScanWarpCoreActivity: Scanning warp core for {ShipName}")]
    static partial void LogActivity(ILogger logger, string ShipName);
}
