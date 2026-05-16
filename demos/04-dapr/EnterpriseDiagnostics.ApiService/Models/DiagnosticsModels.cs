namespace EnterpriseDiagnostics.ApiService.Models;

public record DiagnosticsInput(
    string Id,
    string ShipName,
    string DiagnosticsDate,
    string EngineerName);

public record DiagnosticsOutput(
    string ShipName,
    string DiagnosticsDate,
    string EngineerName,
    RecommendationsResult Recommendations,
    string BridgeNotification);

public record AnalysisInput(
    string ShipName,
    string DiagnosticsDate,
    string EngineerName,
    string SystemName);

public record AnalysisResult(
    string SystemName,
    string Status,
    string[] Issues,
    int HealthPercentage);

public record RecommendationsInput(
    string ShipName,
    string DiagnosticsDate,
    string EngineerName,
    AnalysisResult? HullAnalysis,
    AnalysisResult? WarpCoreAnalysis,
    AnalysisResult? SecurityProtocolsAnalysis,
    AnalysisResult? WeaponSystemsAnalysis = null,
    AnalysisResult? LifeSupportSystemsAnalysis = null);

public record RecommendationsResult(
    string[] Recommendations,
    string[] Priorities);

public record BridgeNotificationInput(
    string ShipName,
    string EngineerName,
    RecommendationsResult Recommendations);

public record BridgeNotificationResult(string Message);
