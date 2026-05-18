using EnterpriseDiagnostics.Conductor.Models;
using EnterpriseDiagnostics.Conductor.Workers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EnterpriseDiagnostics.Conductor.Tests;

public class PrioritizeIssuesWorkerTests
{
    private static PrioritizeIssuesWorker NewWorker() =>
        new(NullLogger<PrioritizeIssuesWorker>.Instance);

    private static PrioritizeInput InputWithHealths(params int[] healths)
    {
        var scans = healths.Select((h, i) =>
            new ScanResult($"sub_{i}", h, Array.Empty<string>())).ToArray();
        return new PrioritizeInput("USS Test", scans);
    }

    [Theory]
    [InlineData(new[] { 49, 90, 90, 90, 90 }, "red_alert")]
    [InlineData(new[] { 50, 90, 90, 90, 90 }, "yellow_alert")]
    [InlineData(new[] { 74, 90, 90, 90, 90 }, "yellow_alert")]
    [InlineData(new[] { 75, 90, 90, 90, 90 }, "green")]
    [InlineData(new[] { 80, 90, 90, 90, 90 }, "green")]
    [InlineData(new[] { 30, 65, 65, 65, 65 }, "red_alert")]
    public void Severity_is_chosen_from_minimum_health(int[] healths, string expectedSeverity)
    {
        var worker = NewWorker();
        var priority = worker.Prioritize(InputWithHealths(healths));
        Assert.Equal(expectedSeverity, priority.Severity);
    }

    [Fact]
    public void Summary_names_worst_subsystem_and_counts_below_threshold()
    {
        var worker = NewWorker();
        // worst is sub_2 at 30; subs below 75 = sub_0 (40), sub_2 (30), sub_3 (60) = 3
        var input = new PrioritizeInput("USS Test", new[]
        {
            new ScanResult("sub_0", 40, Array.Empty<string>()),
            new ScanResult("sub_1", 80, Array.Empty<string>()),
            new ScanResult("sub_2", 30, Array.Empty<string>()),
            new ScanResult("sub_3", 60, Array.Empty<string>()),
            new ScanResult("sub_4", 90, Array.Empty<string>()),
        });

        var priority = worker.Prioritize(input);

        Assert.Equal("red_alert", priority.Severity);
        Assert.Contains("sub_2", priority.Summary);
        Assert.Contains("30", priority.Summary);
        Assert.Contains("3", priority.Summary);   // 3 of 5 subsystems below threshold
    }

    [Fact]
    public void Throws_when_scans_are_empty()
    {
        var worker = NewWorker();
        Assert.Throws<InvalidOperationException>(() =>
            worker.Prioritize(new PrioritizeInput("USS Test", Array.Empty<ScanResult>())));
    }
}
