using Conductor.Api;
using Conductor.Client;
using Conductor.Client.Extensions;
using Conductor.Client.Models;
using EnterpriseDiagnostics.Conductor.Models;
using EnterpriseDiagnostics.Conductor.Workflows;
using Microsoft.AspNetCore.Http.Json;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// snake_case on the wire for the ASP.NET endpoints
builder.Services.Configure<JsonOptions>(o =>
{
    o.SerializerOptions.PropertyNamingPolicy        = JsonNamingPolicy.SnakeCaseLower;
    o.SerializerOptions.DictionaryKeyPolicy         = JsonNamingPolicy.SnakeCaseLower;
    o.SerializerOptions.PropertyNameCaseInsensitive = true;
});

// Conductor connection configuration
var serverUrl = builder.Configuration["Conductor:ServerUrl"]
    ?? throw new InvalidOperationException("Conductor:ServerUrl missing from configuration");

var conductorConfig = new Configuration { BasePath = serverUrl };

// REST API wrappers used by the registrar and the minimal API endpoints.
// Both accept a Configuration ctor arg — pass the singleton directly.
builder.Services.AddSingleton(conductorConfig);
builder.Services.AddSingleton(new WorkflowResourceApi(conductorConfig));
builder.Services.AddSingleton(new MetadataResourceApi(conductorConfig));

// SDK worker wiring:
//   AddConductorWorker registers IWorkflowTaskClient + IWorkflowTaskCoordinator (transient).
//   WithHostedService() registers WorkflowTaskService as a BackgroundService.
//   WorkflowTaskService.ExecuteAsync calls WorkflowTaskCoordinator.Start(token) which
//   internally calls DiscoverWorkers() — a reflection scan over all loaded assemblies for
//   types decorated with [WorkerTask] at the class level (and methods with [WorkerTask]).
//   Our 7 worker classes all have [WorkerTask] on both the class and the worker method,
//   and a parameterless-compatible constructor (ILogger<T> optional, defaults to NullLogger),
//   so Activator.CreateInstance succeeds without DI involvement.
builder.Services.AddConductorWorker(conductorConfig);
builder.Services.WithHostedService();

// Idempotently register the 7 task defs + the workflow def at startup.
builder.Services.AddHostedService<DiagnosticsWorkflowRegistrar>();

var app = builder.Build();

app.MapPost("/start", (DiagnosticsInput input, WorkflowResourceApi api) =>
{
    var request = new StartWorkflowRequest
    {
        Name          = DiagnosticsWorkflow.Name,
        Version       = DiagnosticsWorkflow.Version,
        CorrelationId = input.Id,
        Input = new Dictionary<string, object>
        {
            ["ship_name"]        = input.ShipName,
            ["diagnostics_date"] = input.DiagnosticsDate,
            ["engineer_name"]    = input.EngineerName,
        }
    };
    var workflowId = api.StartWorkflow(request);
    return Results.Ok(new { workflowId });
});

app.MapGet("/status/{id}", (string id, WorkflowResourceApi api) =>
{
    var execution = api.GetExecutionStatus(id, includeTasks: false);

    // execution.Output is a Dictionary<string, object> whose values are
    // Newtonsoft JObject/JArray/JValue instances — System.Text.Json can't
    // serialize those correctly. Round-trip via Newtonsoft so the wire
    // representation matches what Conductor actually stores.
    var json = Newtonsoft.Json.JsonConvert.SerializeObject(new
    {
        status = execution.Status.ToString(),
        output = execution.Output
    });

    return Results.Content(json, "application/json");
});

app.MapPost("/terminate/{id}", (string id, WorkflowResourceApi api) =>
{
    api.Terminate(id, "manual termination");
    return Results.NoContent();
});

app.Run("http://localhost:5050");
