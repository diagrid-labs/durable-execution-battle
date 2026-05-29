using Microsoft.Extensions.Logging;
using Dapr.Workflow;
using EnterpriseDiagnostics.ApiService.Activities;
using EnterpriseDiagnostics.ApiService.Models;

namespace EnterpriseDiagnostics.ApiService.Workflows;

internal sealed partial class DiagnosticsWorkflow : Workflow<DiagnosticsInput, DiagnosticsOutput>
{
    public override async Task<DiagnosticsOutput> RunAsync(
        WorkflowContext context,
        DiagnosticsInput input)
    {
        var logger = context.CreateReplaySafeLogger<DiagnosticsWorkflow>();
        LogStart(logger, context.InstanceId, input.ShipName);

        // ── Fan-out: 5 parallel subsystem scans ──
        var scanTasks = new[]
        {
            context.CallActivityAsync<ScanResult>(
                nameof(ScanWarpCoreActivity),
                new ScanInput(input.ShipName, "warp_core"),
                GetActivityRetryPolicy()),
            context.CallActivityAsync<ScanResult>(
                nameof(ScanShieldsActivity),
                new ScanInput(input.ShipName, "shields"),
                GetActivityRetryPolicy()),
            context.CallActivityAsync<ScanResult>(
                nameof(ScanLifeSupportActivity),
                new ScanInput(input.ShipName, "life_support"),
                GetActivityRetryPolicy()),
            context.CallActivityAsync<ScanResult>(
                nameof(ScanSensorsActivity),
                new ScanInput(input.ShipName, "sensors"),
                GetActivityRetryPolicy()),
            context.CallActivityAsync<ScanResult>(
                nameof(ScanWeaponsActivity),
                new ScanInput(input.ShipName, "weapons"),
                GetActivityRetryPolicy()),
        };

        await Task.WhenAll(scanTasks);
        var scans = scanTasks.Select(t => t.Result).ToArray();
        LogScansComplete(logger, context.InstanceId);

        // ── 60s durable sleep — ⚡ KILL POINT ⚡ ──
        if (!input.Testing)
        {
            LogSleeping(logger, context.InstanceId);
            await context.CreateTimer(TimeSpan.FromSeconds(60));
        }
        else
        {
            LogSleepSkipped(logger, context.InstanceId);
        }

        // ── Prioritization ──
        var priority = await context.CallActivityAsync<Priority>(
            nameof(PrioritizeIssuesActivity),
            new PrioritizeInput(input.ShipName, scans),
            GetActivityRetryPolicy());

        LogPrioritized(logger, context.InstanceId, priority.Severity);

        // ── Conditional bridge notification ──
        string? bridgeMessage = null;
        if (priority.Severity is "yellow_alert" or "red_alert")
        {
            var notification = await context.CallActivityAsync<BridgeNotificationResult>(
                nameof(NotifyBridgeActivity),
                new BridgeNotificationInput(input.ShipName, input.EngineerName, priority),
                GetActivityRetryPolicy());
            bridgeMessage = notification.Message;
        }

        LogWorkflowComplete(logger, context.InstanceId);

        return new DiagnosticsOutput(
            input.ShipName,
            input.DiagnosticsDate,
            input.EngineerName,
            priority,
            bridgeMessage);
    }

    private static WorkflowTaskOptions GetActivityRetryPolicy()
    {
        return new WorkflowTaskOptions
        {
            RetryPolicy = new WorkflowRetryPolicy(
                maxNumberOfAttempts: 5,
                firstRetryInterval: TimeSpan.FromSeconds(3),
                backoffCoefficient: 1.5)
        };
    }

    [LoggerMessage(LogLevel.Information, "Starting diagnostics workflow {Id} for ship: {ShipName}")]
    static partial void LogStart(ILogger logger, string Id, string ShipName);

    [LoggerMessage(LogLevel.Information, "All scans complete for workflow {Id}")]
    static partial void LogScansComplete(ILogger logger, string Id);

    [LoggerMessage(LogLevel.Information, "Workflow {Id} sleeping 60s — engineering review window")]
    static partial void LogSleeping(ILogger logger, string Id);

    [LoggerMessage(LogLevel.Information, "Workflow {Id} prioritized with severity: {Severity}")]
    static partial void LogPrioritized(ILogger logger, string Id, string Severity);

    [LoggerMessage(LogLevel.Information, "Diagnostics workflow {Id} complete")]
    static partial void LogWorkflowComplete(ILogger logger, string Id);

    [LoggerMessage(LogLevel.Information, "Workflow {Id} testing=true — skipping engineering review wait")]
    static partial void LogSleepSkipped(ILogger logger, string Id);
}
