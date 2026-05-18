using Conductor.Client.Worker;
using EnterpriseDiagnostics.Conductor.Models;
using Microsoft.Extensions.Logging.Abstractions;
using ConductorTask = Conductor.Client.Models.Task;

namespace EnterpriseDiagnostics.Conductor.Workers;

[WorkerTask]
public class ScanLifeSupportWorker
{
    private readonly ILogger<ScanLifeSupportWorker> _logger;

    // Parameterless constructor used by SDK's Activator.CreateInstance-based auto-discovery.
    public ScanLifeSupportWorker() : this(NullLogger<ScanLifeSupportWorker>.Instance) { }

    // DI constructor for explicit registration.
    public ScanLifeSupportWorker(ILogger<ScanLifeSupportWorker> logger)
    {
        _logger = logger;
    }

    private static readonly string[] Candidates =
    [
        "Atmospheric CO2 scrubber efficiency reduced by 4.7% on deck 9",
        "Environmental control humidity regulators showing intermittent faults in crew quarters",
        "Oxygen recycler intake filter contamination detected in main air processor",
        "Thermal regulation imbalance reported across deck 12 corridor sections",
        "Gravity plating fluctuation outside nominal range in cargo bay 4"
    ];

    // Typed worker — what the audience reads on stage and what unit tests call.
    public ScanResult Scan(ScanInput input)
    {
        _logger.LogInformation("ScanLifeSupportWorker: scanning life support for {ShipName}", input.ShipName);
        return MockAnalysisGenerator.Generate(input.Subsystem, Candidates);
    }

    // SDK adapter: ConductorTask in / Dictionary<string, object> out with snake_case keys.
    [WorkerTask(taskType: "scan_life_support", workerId: "life_support_worker", batchSize: 5, pollIntervalMs: 100)]
    public Dictionary<string, object> ScanTask(ConductorTask task)
    {
        var input = WorkerSerialization.ReadInput<ScanInput>(task);
        var result = Scan(input);
        return WorkerSerialization.AsSnakeCaseDict(result);
    }
}
