using Dapr.Workflow;
using EnterpriseDiagnostics.ApiService.Models;

namespace EnterpriseDiagnostics.ApiService.Activities;

internal sealed partial class ScanWeaponsActivity(
    ILogger<ScanWeaponsActivity> logger) : WorkflowActivity<ScanInput, ScanResult>
{
    private static readonly string[] CandidateAnomalies =
    [
        "Phaser array bank 3 power coupling shows intermittent drop",
        "Photon torpedo launcher loading mechanism response time elevated",
        "Targeting sensor calibration drift detected on aft array",
        "Tactical computer subroutine queue depth exceeding threshold",
        "Quantum torpedo magazine humidity outside recommended range"
    ];

    public override Task<ScanResult> RunAsync(
        WorkflowActivityContext context,
        ScanInput input)
    {
        LogActivity(logger, input.ShipName);
        return Task.FromResult(MockAnalysisGenerator.Generate(input.Subsystem, CandidateAnomalies));
    }

    [LoggerMessage(LogLevel.Information, "ScanWeaponsActivity: Scanning weapons for {ShipName}")]
    static partial void LogActivity(ILogger logger, string ShipName);
}
