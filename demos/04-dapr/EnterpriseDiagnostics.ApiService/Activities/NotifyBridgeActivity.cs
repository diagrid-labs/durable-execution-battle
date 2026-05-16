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

        var message =
            $"🚨 BRIDGE ALERT — {input.ShipName} — {input.Priority.Severity.ToUpperInvariant()}: " +
            $"{input.Priority.Summary} (engineer: {input.EngineerName})";

        LogNotification(logger, message);

        return Task.FromResult(new BridgeNotificationResult(message));
    }

    [LoggerMessage(LogLevel.Information, "NotifyBridgeActivity: Sending bridge notification for {ShipName}")]
    static partial void LogActivity(ILogger logger, string ShipName);

    [LoggerMessage(LogLevel.Information, "Bridge notification: {Message}")]
    static partial void LogNotification(ILogger logger, string Message);
}
