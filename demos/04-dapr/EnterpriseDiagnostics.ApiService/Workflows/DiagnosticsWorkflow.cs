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

        // Define activity tasks to run the five analyses in parallel
        var hullTask = context.CallActivityAsync<AnalysisResult>(
            nameof(AnalyzeHullActivity),
            new AnalysisInput(input.ShipName, input.DiagnosticsDate, input.EngineerName, "Hull"),
            GetActivityRetryPolicy());

        var warpCoreTask = context.CallActivityAsync<AnalysisResult>(
            nameof(AnalyzeWarpCoreActivity),
            new AnalysisInput(input.ShipName, input.DiagnosticsDate, input.EngineerName, "Warp Core"),
            GetActivityRetryPolicy());

        var securityTask = context.CallActivityAsync<AnalysisResult>(
            nameof(AnalyzeSecuritySystemsActivity),
            new AnalysisInput(input.ShipName, input.DiagnosticsDate, input.EngineerName, "Security Systems"),
            GetActivityRetryPolicy());

        var weaponsTask = context.CallActivityAsync<AnalysisResult>(
            nameof(AnalyzeWeaponSystemsActivity),
            new AnalysisInput(input.ShipName, input.DiagnosticsDate, input.EngineerName, "Weapon Systems"),
            GetActivityRetryPolicy());

        var lifeSupportTask = context.CallActivityAsync<AnalysisResult>(
            nameof(AnalyzeLifeSupportSystemsActivity),
            new AnalysisInput(input.ShipName, input.DiagnosticsDate, input.EngineerName, "Life Support Systems"),
            GetActivityRetryPolicy());

        // Fan-out/fan-in: wait for all analyses to complete
        await Task.WhenAll(hullTask, warpCoreTask, securityTask, weaponsTask, lifeSupportTask);

        var hullResult = hullTask.Result;
        var warpCoreResult = warpCoreTask.Result;
        var securityResult = securityTask.Result;
        var weaponsResult = weaponsTask.Result;
        var lifeSupportResult = lifeSupportTask.Result;

        LogAnalysesComplete(logger, context.InstanceId);

        // Generate recommendations based on combined analysis data
        var recommendationsInput = new RecommendationsInput(
            input.ShipName,
            input.DiagnosticsDate,
            input.EngineerName,
            hullResult,
            warpCoreResult,
            securityResult,
            weaponsResult,
            lifeSupportResult);

        var recommendations = await context.CallActivityAsync<RecommendationsResult>(
            nameof(GenerateRecommendationsActivity),
            recommendationsInput,
            GetActivityRetryPolicy());

        LogRecommendationsComplete(logger, context.InstanceId);

        // Notify the bridge with results
        var notificationInput = new BridgeNotificationInput(
            input.ShipName,
            input.EngineerName,
            recommendations);

        var notification = await context.CallActivityAsync<BridgeNotificationResult>(
            nameof(NotifyBridgeActivity),
            notificationInput,
            GetActivityRetryPolicy());

        LogWorkflowComplete(logger, context.InstanceId);

        return new DiagnosticsOutput(
            input.ShipName,
            input.DiagnosticsDate,
            input.EngineerName,
            recommendations,
            notification.Message);
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

    [LoggerMessage(LogLevel.Information, "All analyses complete for workflow {Id}")]
    static partial void LogAnalysesComplete(ILogger logger, string Id);

    [LoggerMessage(LogLevel.Information, "Recommendations generated for workflow {Id}")]
    static partial void LogRecommendationsComplete(ILogger logger, string Id);

    [LoggerMessage(LogLevel.Information, "Diagnostics workflow {Id} complete")]
    static partial void LogWorkflowComplete(ILogger logger, string Id);
}
