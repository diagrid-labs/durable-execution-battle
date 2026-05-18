using Conductor.Client.Worker;
using EnterpriseDiagnostics.Conductor.Models;
using Microsoft.Extensions.Logging.Abstractions;
using ConductorTask = Conductor.Client.Models.Task;

namespace EnterpriseDiagnostics.Conductor.Workers;

[WorkerTask]
public class NotifyBridgeWorker
{
    private readonly ILogger<NotifyBridgeWorker> _logger;

    // Parameterless constructor used by SDK's Activator.CreateInstance-based auto-discovery.
    public NotifyBridgeWorker() : this(NullLogger<NotifyBridgeWorker>.Instance) { }

    // DI constructor for explicit registration.
    public NotifyBridgeWorker(ILogger<NotifyBridgeWorker> logger)
    {
        _logger = logger;
    }

    // Typed worker — what the audience reads on stage and what unit tests call.
    public BridgeNotificationResult Notify(BridgeNotificationInput input)
    {
        var message =
            $"🚨 BRIDGE ALERT — {input.ShipName} — {input.Priority.Severity.ToUpperInvariant()}: " +
            $"{input.Priority.Summary} (engineer: {input.EngineerName})";

        _logger.LogWarning("{Message}", message);
        // Stage payoff: the SDK uses Activator.CreateInstance (no DI), so the injected
        // logger is NullLogger and LogWarning is swallowed. Emit to Console so the
        // alert is visible in the running app's stdout during the live demo.
        Console.WriteLine();
        Console.WriteLine(message);
        Console.WriteLine();

        return new BridgeNotificationResult(message);
    }

    // SDK adapter: ConductorTask in / Dictionary<string, object> out with snake_case keys.
    [WorkerTask(taskType: "notify_bridge", workerId: "bridge_worker", batchSize: 5, pollIntervalMs: 100)]
    public Dictionary<string, object> NotifyTask(ConductorTask task)
    {
        var input = WorkerSerialization.ReadInput<BridgeNotificationInput>(task);
        var result = Notify(input);
        return WorkerSerialization.AsSnakeCaseDict(result);
    }
}
