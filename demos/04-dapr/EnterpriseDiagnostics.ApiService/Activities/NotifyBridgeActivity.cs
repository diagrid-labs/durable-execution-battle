using Dapr.Workflow;
using EnterpriseDiagnostics.ApiService.Models;

namespace EnterpriseDiagnostics.ApiService.Activities;

internal sealed partial class NotifyBridgeActivity(
    ILogger<NotifyBridgeActivity> logger) : WorkflowActivity<BridgeNotificationInput, BridgeNotificationResult>
{
    public override Task<BridgeNotificationResult> RunAsync(
        WorkflowActivityContext context,
        BridgeNotificationInput input)
    {
        LogActivity(logger, input.ShipName);

        var topPriority = input.Recommendations.Priorities.FirstOrDefault()
            ?? "No prioritized items.";
        var recommendationCount = input.Recommendations.Recommendations.Length;

        var message =
            $"Bridge notification for the {input.ShipName}: " +
            $"diagnostics requested by {input.EngineerName} are complete. " +
            $"{recommendationCount} recommendation(s) generated. " +
            $"Top priority: {topPriority}.";

        LogNotification(logger, message);

        return Task.FromResult(new BridgeNotificationResult(message));
    }

    [LoggerMessage(LogLevel.Information, "NotifyBridgeActivity: Sending bridge notification for {ShipName}")]
    static partial void LogActivity(ILogger logger, string ShipName);

    [LoggerMessage(LogLevel.Information, "Bridge notification: {Message}")]
    static partial void LogNotification(ILogger logger, string Message);
}
