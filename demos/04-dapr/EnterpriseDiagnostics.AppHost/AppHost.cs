using System.Reflection;
using CommunityToolkit.Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDapr();

string executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
    ?? throw new("Where am I?");

var statePassword = builder.AddParameter("cache-password", "state-store-123", secret: true);
var workflowState = builder
    .AddValkey("workflow-state", 16379, statePassword)
    .WithContainerName("workflow-state")
    .WithDataVolume("workflow-state-data");

var workflowApp = builder
    .AddProject<Projects.EnterpriseDiagnostics_ApiService>("wf-app")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        LogLevel = "debug",
        ResourcesPaths =
        [
            Path.Join(executingPath, "Resources"),
        ],
    });

workflowApp.WaitFor(workflowState);

builder
    .AddContainer("diagrid-dashboard", "ghcr.io/diagridio/diagrid-dashboard:latest")
    .WithContainerName("diagrid-dashboard")
    .WithBindMount(Path.Join(executingPath, "Resources/dashboard"), "/app/components")
    .WithEnvironment("COMPONENT_FILE", "/app/components/statestore-dashboard.yaml")
    .WithEnvironment("APP_ID", "diagrid-dashboard")
    .WithHttpEndpoint(port: 58888, targetPort: 8080)
    .WithReference(workflowState);

builder.Build().Run();
