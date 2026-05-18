using Conductor.Api;
using Conductor.Client;
using Conductor.Client.Models;
using SystemTask = System.Threading.Tasks.Task;

namespace EnterpriseDiagnostics.Conductor.Workflows;

public class DiagnosticsWorkflowRegistrar(
    MetadataResourceApi metadataApi,
    ILogger<DiagnosticsWorkflowRegistrar> logger) : IHostedService
{
    private static readonly string[] TaskTypes =
    {
        "scan_warp_core", "scan_shields", "scan_life_support",
        "scan_sensors",   "scan_weapons",
        "prioritize_issues", "notify_bridge"
    };

    public SystemTask StartAsync(CancellationToken cancellationToken)
    {
        var taskDefs = TaskTypes.Select(name => new TaskDef
        {
            Name                   = name,
            RetryCount             = 3,
            TimeoutSeconds         = 30,
            ResponseTimeoutSeconds = 20,
            OwnerEmail             = "marc@diagrid.io"
        }).ToList();

        try
        {
            metadataApi.RegisterTaskDef(taskDefs);
            logger.LogInformation("Registered {Count} task definitions", taskDefs.Count);
        }
        catch (ApiException ex) when (ex.ErrorCode == 409)
        {
            foreach (var td in taskDefs)
            {
                metadataApi.UpdateTaskDef(td);
            }
            logger.LogInformation("Updated {Count} existing task definitions", taskDefs.Count);
        }

        // ConductorWorkflow IS a WorkflowDef — pass it directly.
        // UpdateWorkflowDefinitions has PUT semantics: replaces same name+version.
        var workflow = DiagnosticsWorkflow.Build();
        metadataApi.UpdateWorkflowDefinitions(new List<WorkflowDef> { workflow });
        logger.LogInformation(
            "Registered workflow '{Name}' v{Version}",
            DiagnosticsWorkflow.Name, DiagnosticsWorkflow.Version);

        return SystemTask.CompletedTask;
    }

    public SystemTask StopAsync(CancellationToken cancellationToken) => SystemTask.CompletedTask;
}
