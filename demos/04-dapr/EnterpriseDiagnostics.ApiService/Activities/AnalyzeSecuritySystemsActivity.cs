using Dapr.Workflow;
using EnterpriseDiagnostics.ApiService.Models;

namespace EnterpriseDiagnostics.ApiService.Activities;

internal sealed partial class AnalyzeSecuritySystemsActivity(
    ILogger<AnalyzeSecuritySystemsActivity> logger) : WorkflowActivity<AnalysisInput, AnalysisResult>
{
    private static readonly string[] CandidateIssues =
    [
        "Shield generator 2 recharge rate slower than baseline",
        "Encryption protocol due for rotation in less than 48 hours",
        "Intrusion detection system reporting recurring false positives on deck 8",
        "Access control matrix has 3 stale credentials pending revocation",
        "Forcefield emitter coverage gap detected near cargo bay 4"
    ];

    public override Task<AnalysisResult> RunAsync(
        WorkflowActivityContext context,
        AnalysisInput input)
    {
        LogActivity(logger, input.ShipName);

        // Just to simulate some processing, and you have time to pause the workflow.
        Thread.Sleep(2500);

        var result = MockAnalysisGenerator.Generate("Security Systems", CandidateIssues);
        return Task.FromResult(result);
    }

    [LoggerMessage(LogLevel.Information, "AnalyzeSecurityProtocolsActivity: Analyzing security protocols for {ShipName}")]
    static partial void LogActivity(ILogger logger, string ShipName);
}
