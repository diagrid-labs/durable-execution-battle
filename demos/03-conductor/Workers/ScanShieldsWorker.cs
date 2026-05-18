using Conductor.Client.Worker;
using EnterpriseDiagnostics.Conductor.Models;
using Microsoft.Extensions.Logging.Abstractions;
using ConductorTask = Conductor.Client.Models.Task;

namespace EnterpriseDiagnostics.Conductor.Workers;

[WorkerTask]
public class ScanShieldsWorker
{
    private readonly ILogger<ScanShieldsWorker> _logger;

    // Parameterless constructor used by SDK's Activator.CreateInstance-based auto-discovery.
    public ScanShieldsWorker() : this(NullLogger<ScanShieldsWorker>.Instance) { }

    // DI constructor for explicit registration.
    public ScanShieldsWorker(ILogger<ScanShieldsWorker> logger)
    {
        _logger = logger;
    }

    private static readonly string[] Candidates =
    [
        "Deflector grid phase variance exceeding nominal range",
        "Shield generator 2 recharge rate slower than baseline",
        "Forward shield emitter coupling showing micro-arc events",
        "Harmonic resonance detected in dorsal shield projector",
        "EPS tap to aft shield array running 8% under spec"
    ];

    // Typed worker — what the audience reads on stage and what unit tests call.
    public ScanResult Scan(ScanInput input)
    {
        _logger.LogInformation("ScanShieldsWorker: scanning shields for {ShipName}", input.ShipName);
        return MockAnalysisGenerator.Generate(input.Subsystem, Candidates);
    }

    // SDK adapter: ConductorTask in / Dictionary<string, object> out with snake_case keys.
    [WorkerTask(taskType: "scan_shields", workerId: "shields_worker", batchSize: 5, pollIntervalMs: 100)]
    public Dictionary<string, object> ScanTask(ConductorTask task)
    {
        var input = WorkerSerialization.ReadInput<ScanInput>(task);
        var result = Scan(input);
        return WorkerSerialization.AsSnakeCaseDict(result);
    }
}
