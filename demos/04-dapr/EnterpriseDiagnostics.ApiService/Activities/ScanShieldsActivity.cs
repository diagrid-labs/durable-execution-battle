using Dapr.Workflow;
using EnterpriseDiagnostics.ApiService.Models;

namespace EnterpriseDiagnostics.ApiService.Activities;

internal sealed partial class ScanShieldsActivity(
    ILogger<ScanShieldsActivity> logger) : WorkflowActivity<ScanInput, ScanResult>
{
    private static readonly string[] CandidateAnomalies =
    [
        "Deflector grid phase variance exceeding nominal range",
        "Shield generator 2 recharge rate slower than baseline",
        "Forward shield emitter coupling showing micro-arc events",
        "Harmonic resonance detected in dorsal shield projector",
        "EPS tap to aft shield array running 8% under spec"
    ];

    public override Task<ScanResult> RunAsync(
        WorkflowActivityContext context,
        ScanInput input)
    {
        LogActivity(logger, input.ShipName);
        return Task.FromResult(MockAnalysisGenerator.Generate(input.Subsystem, CandidateAnomalies));
    }

    [LoggerMessage(LogLevel.Information, "ScanShieldsActivity: Scanning shields for {ShipName}")]
    static partial void LogActivity(ILogger logger, string ShipName);
}
