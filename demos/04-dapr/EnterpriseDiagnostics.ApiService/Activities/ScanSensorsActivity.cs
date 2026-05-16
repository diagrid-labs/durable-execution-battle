using Dapr.Workflow;
using EnterpriseDiagnostics.ApiService.Models;

namespace EnterpriseDiagnostics.ApiService.Activities;

internal sealed partial class ScanSensorsActivity(
    ILogger<ScanSensorsActivity> logger) : WorkflowActivity<ScanInput, ScanResult>
{
    private static readonly string[] CandidateAnomalies =
    [
        "Lateral sensor array gain calibration drift detected",
        "Subspace bandwidth allocation below recommended threshold",
        "Long-range sensor pallet 3 reporting elevated noise floor",
        "Navigational deflector return signal showing phase jitter",
        "Tachyon detection grid misaligned by 0.12 degrees"
    ];

    public override Task<ScanResult> RunAsync(
        WorkflowActivityContext context,
        ScanInput input)
    {
        LogActivity(logger, input.ShipName);
        return Task.FromResult(MockAnalysisGenerator.Generate(input.Subsystem, CandidateAnomalies));
    }

    [LoggerMessage(LogLevel.Information, "ScanSensorsActivity: Scanning sensors for {ShipName}")]
    static partial void LogActivity(ILogger logger, string ShipName);
}
