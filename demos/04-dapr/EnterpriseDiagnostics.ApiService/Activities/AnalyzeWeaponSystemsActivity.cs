using Dapr.Workflow;
using EnterpriseDiagnostics.ApiService.Models;

namespace EnterpriseDiagnostics.ApiService.Activities;

internal sealed partial class AnalyzeWeaponSystemsActivity(
    ILogger<AnalyzeWeaponSystemsActivity> logger) : WorkflowActivity<AnalysisInput, AnalysisResult>
{
    private static readonly string[] CandidateIssues =
    [
        "Phaser array bank 3 power coupling shows intermittent drop",
        "Photon torpedo launcher loading mechanism response time elevated",
        "Targeting sensor calibration drift detected on aft array",
        "Tactical computer subroutine queue depth exceeding threshold",
        "Quantum torpedo magazine humidity outside recommended range"
    ];

    public override Task<AnalysisResult> RunAsync(
        WorkflowActivityContext context,
        AnalysisInput input)
    {
        LogActivity(logger, input.ShipName);

        // Just to simulate some processing, and you have time to pause the workflow.
        Thread.Sleep(2500);

        var result = MockAnalysisGenerator.Generate("Weapon Systems", CandidateIssues);
        return Task.FromResult(result);
    }

    [LoggerMessage(LogLevel.Information, "AnalyzeWeaponSystemsActivity: Analyzing weapon systems for {ShipName}")]
    static partial void LogActivity(ILogger logger, string ShipName);
}
