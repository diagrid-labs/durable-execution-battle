using Conductor.Client.Worker;
using EnterpriseDiagnostics.Conductor.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ConductorTask = Conductor.Client.Models.Task;

namespace EnterpriseDiagnostics.Conductor.Workers;

[WorkerTask]
public class PrioritizeIssuesWorker
{
    private readonly ILogger<PrioritizeIssuesWorker> _logger;

    // Parameterless constructor used by SDK's Activator.CreateInstance-based auto-discovery.
    public PrioritizeIssuesWorker() : this(NullLogger<PrioritizeIssuesWorker>.Instance) { }

    // DI constructor for explicit registration.
    public PrioritizeIssuesWorker(ILogger<PrioritizeIssuesWorker> logger)
    {
        _logger = logger;
    }

    // Typed worker — what the audience reads on stage and what unit tests call.
    public Priority Prioritize(PrioritizeInput input)
    {
        _logger.LogInformation(
            "PrioritizeIssuesWorker: prioritizing issues for {ShipName}", input.ShipName);

        if (input.Scans.Length == 0)
            throw new InvalidOperationException("scans cannot be empty");

        var worst = input.Scans.OrderBy(s => s.Health).First();
        var belowThreshold = input.Scans.Count(s => s.Health < 75);

        var severity = worst.Health switch
        {
            < 50 => Severity.RedAlert,
            < 75 => Severity.YellowAlert,
            _    => Severity.Green
        };

        var summary =
            $"Worst subsystem: {worst.Subsystem} @ {worst.Health}% health. " +
            $"{belowThreshold} of {input.Scans.Length} subsystems below operational threshold.";

        return new Priority(severity, summary);
    }

    // SDK adapter: ConductorTask in / Dictionary<string, object> out with snake_case keys.
    [WorkerTask(taskType: "prioritize_issues", workerId: "prioritize_worker", batchSize: 5, pollIntervalMs: 100)]
    public Dictionary<string, object> PrioritizeTask(ConductorTask task)
    {
        var shipName = task.InputData["ship_name"]?.ToString() ?? "";

        // scans arrive as a JArray of snake_case scan output dicts
        var scansRaw = task.InputData["scans"];
        var scansJson = JsonConvert.SerializeObject(scansRaw);
        var scans = JsonConvert.DeserializeObject<ScanResult[]>(scansJson)
            ?? throw new InvalidOperationException("scans cannot be null");

        var input = new PrioritizeInput(shipName, scans);
        var result = Prioritize(input);
        return WorkerSerialization.AsSnakeCaseDict(result);
    }
}
