using Dapr.Workflow;
using EnterpriseDiagnostics.ApiService.Models;

namespace EnterpriseDiagnostics.ApiService.Activities;

internal sealed partial class PrioritizeIssuesActivity(
    ILogger<PrioritizeIssuesActivity> logger) : WorkflowActivity<PrioritizeInput, Priority>
{
    public override Task<Priority> RunAsync(
        WorkflowActivityContext context,
        PrioritizeInput input)
    {
        LogActivity(logger, input.ShipName);

        var worst = input.Scans.OrderBy(s => s.Health).First();
        var minHealth = worst.Health;
        var belowOperational = input.Scans.Count(s => s.Health < 75);

        var severity = minHealth switch
        {
            < 50 => "red_alert",
            < 75 => "yellow_alert",
            _    => "green"
        };

        var summary =
            $"Worst subsystem: {worst.Subsystem} @ {worst.Health}% health. " +
            $"{belowOperational} of {input.Scans.Length} subsystems below operational threshold.";

        return Task.FromResult(new Priority(severity, summary));
    }

    [LoggerMessage(LogLevel.Information, "PrioritizeIssuesActivity: Prioritizing issues for {ShipName}")]
    static partial void LogActivity(ILogger logger, string ShipName);
}
