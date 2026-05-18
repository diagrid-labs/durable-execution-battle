using Conductor.Client.Worker;
using EnterpriseDiagnostics.Conductor.Models;
using Microsoft.Extensions.Logging.Abstractions;
using ConductorTask = Conductor.Client.Models.Task;

namespace EnterpriseDiagnostics.Conductor.Workers;

[WorkerTask]
public class ScanWarpCoreWorker
{
    private readonly ILogger<ScanWarpCoreWorker> _logger;

    // Parameterless constructor used by SDK's Activator.CreateInstance-based auto-discovery.
    public ScanWarpCoreWorker() : this(NullLogger<ScanWarpCoreWorker>.Instance) { }

    // DI constructor for explicit registration.
    public ScanWarpCoreWorker(ILogger<ScanWarpCoreWorker> logger)
    {
        _logger = logger;
    }

    private static readonly string[] Candidates =
    [
        "Matter/antimatter containment field oscillating outside tolerance",
        "Dilithium crystal alignment drift detected at 0.04 microns",
        "Plasma injector #3 throughput below specification",
        "Warp field coil efficiency reduced by 6% on starboard nacelle",
        "EPS conduit overheating near main reactor manifold"
    ];

    // Typed worker — what the audience reads on stage and what unit tests call.
    public ScanResult Scan(ScanInput input)
    {
        _logger.LogInformation("ScanWarpCoreWorker: scanning warp core for {ShipName}", input.ShipName);
        return MockAnalysisGenerator.Generate(input.Subsystem, Candidates);
    }

    // SDK adapter: ConductorTask in / Dictionary<string, object> out with snake_case keys.
    [WorkerTask(taskType: "scan_warp_core", workerId: "warp_core_worker", batchSize: 5, pollIntervalMs: 100)]
    public Dictionary<string, object> ScanTask(ConductorTask task)
    {
        var input = WorkerSerialization.ReadInput<ScanInput>(task);
        var result = Scan(input);
        return WorkerSerialization.AsSnakeCaseDict(result);
    }
}
