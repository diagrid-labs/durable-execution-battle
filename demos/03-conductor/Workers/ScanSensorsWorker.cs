using Conductor.Client.Worker;
using EnterpriseDiagnostics.Conductor.Models;
using Microsoft.Extensions.Logging.Abstractions;
using ConductorTask = Conductor.Client.Models.Task;

namespace EnterpriseDiagnostics.Conductor.Workers;

[WorkerTask]
public class ScanSensorsWorker
{
    private readonly ILogger<ScanSensorsWorker> _logger;

    // Parameterless constructor used by SDK's Activator.CreateInstance-based auto-discovery.
    public ScanSensorsWorker() : this(NullLogger<ScanSensorsWorker>.Instance) { }

    // DI constructor for explicit registration.
    public ScanSensorsWorker(ILogger<ScanSensorsWorker> logger)
    {
        _logger = logger;
    }

    private static readonly string[] Candidates =
    [
        "Lateral sensor array gain calibration drift detected",
        "Subspace bandwidth allocation below recommended threshold",
        "Long-range sensor pallet 3 reporting elevated noise floor",
        "Navigational deflector return signal showing phase jitter",
        "Tachyon detection grid misaligned by 0.12 degrees"
    ];

    // Typed worker — what the audience reads on stage and what unit tests call.
    public ScanResult Scan(ScanInput input)
    {
        _logger.LogInformation("ScanSensorsWorker: scanning sensors for {ShipName}", input.ShipName);
        return MockAnalysisGenerator.Generate(input.Subsystem, Candidates);
    }

    // SDK adapter: ConductorTask in / Dictionary<string, object> out with snake_case keys.
    [WorkerTask(taskType: "scan_sensors", workerId: "sensors_worker", batchSize: 5, pollIntervalMs: 100)]
    public Dictionary<string, object> ScanTask(ConductorTask task)
    {
        var input = WorkerSerialization.ReadInput<ScanInput>(task);
        var result = Scan(input);
        return WorkerSerialization.AsSnakeCaseDict(result);
    }
}
