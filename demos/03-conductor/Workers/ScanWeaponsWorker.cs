using Conductor.Client.Worker;
using EnterpriseDiagnostics.Conductor.Models;
using Microsoft.Extensions.Logging.Abstractions;
using ConductorTask = Conductor.Client.Models.Task;

namespace EnterpriseDiagnostics.Conductor.Workers;

[WorkerTask]
public class ScanWeaponsWorker
{
    private readonly ILogger<ScanWeaponsWorker> _logger;

    // Parameterless constructor used by SDK's Activator.CreateInstance-based auto-discovery.
    public ScanWeaponsWorker() : this(NullLogger<ScanWeaponsWorker>.Instance) { }

    // DI constructor for explicit registration.
    public ScanWeaponsWorker(ILogger<ScanWeaponsWorker> logger)
    {
        _logger = logger;
    }

    private static readonly string[] Candidates =
    [
        "Phaser array bank 3 power coupling shows intermittent drop",
        "Photon torpedo launcher loading mechanism response time elevated",
        "Targeting sensor calibration drift detected on aft array",
        "Tactical computer subroutine queue depth exceeding threshold",
        "Quantum torpedo magazine humidity outside recommended range"
    ];

    // Typed worker — what the audience reads on stage and what unit tests call.
    public ScanResult Scan(ScanInput input)
    {
        _logger.LogInformation("ScanWeaponsWorker: scanning weapons for {ShipName}", input.ShipName);
        return MockAnalysisGenerator.Generate(input.Subsystem, Candidates);
    }

    // SDK adapter: ConductorTask in / Dictionary<string, object> out with snake_case keys.
    [WorkerTask(taskType: "scan_weapons", workerId: "weapons_worker", batchSize: 5, pollIntervalMs: 100)]
    public Dictionary<string, object> ScanTask(ConductorTask task)
    {
        var input = WorkerSerialization.ReadInput<ScanInput>(task);
        var result = Scan(input);
        return WorkerSerialization.AsSnakeCaseDict(result);
    }
}
