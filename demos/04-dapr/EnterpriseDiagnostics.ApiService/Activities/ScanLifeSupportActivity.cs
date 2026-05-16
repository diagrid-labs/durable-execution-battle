using Dapr.Workflow;
using EnterpriseDiagnostics.ApiService.Models;

namespace EnterpriseDiagnostics.ApiService.Activities;

internal sealed partial class ScanLifeSupportActivity(
    ILogger<ScanLifeSupportActivity> logger) : WorkflowActivity<ScanInput, ScanResult>
{
    private static readonly string[] CandidateAnomalies =
    [
        "Atmospheric CO2 scrubber efficiency reduced by 4.7% on deck 9",
        "Environmental control humidity regulators showing intermittent faults in crew quarters",
        "Oxygen recycler intake filter contamination detected in main air processor",
        "Thermal regulation imbalance reported across deck 12 corridor sections",
        "Gravity plating fluctuation outside nominal range in cargo bay 4"
    ];

    public override Task<ScanResult> RunAsync(
        WorkflowActivityContext context,
        ScanInput input)
    {
        LogActivity(logger, input.ShipName);
        return Task.FromResult(MockAnalysisGenerator.Generate(input.Subsystem, CandidateAnomalies));
    }

    [LoggerMessage(LogLevel.Information, "ScanLifeSupportActivity: Scanning life support for {ShipName}")]
    static partial void LogActivity(ILogger logger, string ShipName);
}
