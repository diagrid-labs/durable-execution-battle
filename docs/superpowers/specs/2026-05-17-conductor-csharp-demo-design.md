# Conductor (C#) demo — design

**Status:** approved design, ready for implementation plan
**Author:** Marc Duiker
**Date:** 2026-05-17
**Target:** `demos/03-conductor/`
**Canonical scenario:** [`demos/shared/scenario.md`](../../../demos/shared/scenario.md) (mirrored verbatim from `plan.md` §4)
**Canonical reference implementation:** [`demos/04-dapr/`](../../../demos/04-dapr/) (.NET 10 + Aspire — see `plan.md` §5.4)
**Sister-language demo:** [`demos/04-dapr/`](../../../demos/04-dapr/). Same host language (C# on .NET 10), opposite philosophy — Conductor's workflow is JSON the engine interprets, Dapr's is C# the runtime replays. That deliberate pairing is the §5.0/§5.3 talking point this spec exists to honor.

---

## 1. Goal

Implement the USS Enterprise diagnostics workflow in **Conductor with C#** as one of the five framework demos for the "Durable Execution Battle" talk. The implementation must:

- Match the **canonical shared scenario** (§4 of `plan.md`) on activity *behavior*, severity thresholds, rigging strategy, and the kill-and-resume choreography — even though the workflow itself is data, not code.
- Hold the **cross-demo invariants** from `plan.md` §5.0 constant on the wire — task names (`scan_warp_core`, `scan_shields`, `scan_life_support`, `scan_sensors`, `scan_weapons`, `prioritize_issues`, `notify_bridge`), severity wire values (`green`, `yellow_alert`, `red_alert`), health range `30..65`, bridge alert message format, and workflow id `enterprise-ncc-1701`.
- Be **idiomatic Conductor + C#** — the workflow is a `ConductorWorkflow` built with the fluent C# SDK (`ForkJoinTask`, `WaitTask`, `SwitchTask`); workers are methods decorated with `[WorkerTask]` attributes hosted by a `WorkflowTaskCoordinator` running as an `IHostedService`. **No `Task.WhenAll`** appears in the workflow definition — the engine schedules the FORK_JOIN branches.
- Run from a fresh laptop in under 3 minutes (`docker compose up` for `conductor-standalone`, then `dotnet run`).
- **Be the workflow-as-data demo.** When the audience watches Dapr immediately after, the on-stage payoff is: same C# language, opposite mental model. Conductor's fluent C# *builds* JSON; the JSON *is* the workflow. Dapr's C# *is* the workflow.

## 2. Locked decisions

| Choice | Decision | Why |
|---|---|---|
| **Language / runtime** | C# / .NET 10 | Same host language as the Dapr demo two slots later — see `plan.md` §5.0 deliberate-pairing rationale |
| **Build tool** | `dotnet` CLI, single `.csproj` (no Aspire) | Aspire is the Dapr demo's flourish; Conductor doesn't need orchestration of multiple .NET services. One `.csproj` keeps the on-stage IDE simple. |
| **HTTP framework** | ASP.NET Core minimal API | Lowest-ceremony way to expose `POST /start` + `GET /status`. Mirrors the Dapr demo's API surface so the comparison reads cleanly. |
| **Conductor SDK** | `conductor-csharp` (Orkes-maintained NuGet package; pin latest stable pre-talk) | First-party C# SDK; provides both the fluent workflow builder and the `[WorkerTask]` attribute + `WorkflowTaskCoordinator` host |
| **Workflow definition strategy** | **Fluent C# builder, registered at startup** via `MetadataResourceApi.UpdateAsync(...)` from a dedicated `IHostedService` (idempotent register-or-update — Conductor's metadata API replaces an existing definition at the same version) | The §5.3 reveal beat: open Conductor UI → Workflow Definitions → show the generated JSON; flip back to `DiagnosticsWorkflow.cs` and point at the line-for-line correspondence. The C# fluent code IS the talking point; the JSON is the receipt. |
| **Conductor server** | `conductoross/conductor-standalone:v3.30.0.rc15` via `docker-compose.yml` (single image — includes server + UI + Elasticsearch) | Cleanest on-stage start; no multi-container choreography. Pin the tag; verify the image still pulls a week before the talk (see §15). |
| **Conductor server port** | `8080` (REST) + `5000` (UI) | Conductor standalone defaults. The ASP.NET app uses **`5050`** to avoid the UI port clash. |
| **ASP.NET port** | `5050` (HTTP only — no HTTPS for stage demo) | Avoids the `5000` clash with the Conductor UI |
| **Wire format** | snake_case on the wire (Conductor's convention) via `JsonNamingPolicy.SnakeCaseLower` in `Program.cs` | Keeps C# records PascalCase in source, snake_case in JSON. Severity wire values (`red_alert`, `yellow_alert`, `green`) match Dapr demo verbatim. |
| **Project layout** | Single C# project under `demos/03-conductor/` | Mirrors Dapr's single-API-service surface; no AppHost split |
| **Worker hosting** | One `WorkflowTaskCoordinator` registered as `IHostedService`; all 7 workers (5 scans + prioritize + notify) live in `[WorkerTask]`-decorated methods on per-task worker classes in `Workers/` | Matches `plan.md` §5.3 layout verbatim |
| **Fan-out output shape** | `prioritize_issues` task's `scans` input parameter is built explicitly from each scan task's output: `["${scan_warp_core.output}", ..., "${scan_weapons.output}"]` | Cleaner and more legible than relying on FORK_JOIN aggregate output; the array is visible in the workflow definition JSON and easy to point at on stage |
| **SWITCH evaluator** | `value-param` evaluator over `${prioritize.output.severity}`; two `decisionCases` (`red_alert` and `yellow_alert`) both call `notify_bridge`; `green` is the default case (empty) | Avoids the `javascript` evaluator (extra runtime layer); cases match severity wire values exactly. Two switch-case branches keep the JSON readable on stage. |

## 3. Project layout

```
demos/03-conductor/
├── README.md                              # run instructions + talk choreography
├── EnterpriseDiagnostics.Conductor.csproj # single project
├── Program.cs                             # minimal API + IHostedService registrations
├── appsettings.json                       # Conductor server URL, polling intervals
├── appsettings.Development.json
├── docker-compose.yml                     # conductoross/conductor-standalone (server + UI + ES)
├── enterprise-diagnostics.http            # POST /start, GET /status/{id}, terminate — VS Code REST Client
│
├── Workflows/
│   ├── DiagnosticsWorkflow.cs             # static Build() → ConductorWorkflow (fluent FORK_JOIN + WAIT + SWITCH)
│   └── DiagnosticsWorkflowRegistrar.cs    # IHostedService: POSTs the built definition via MetadataResourceApi at startup
│
├── Workers/
│   ├── ScanWarpCoreWorker.cs              # [WorkerTask("scan_warp_core")]
│   ├── ScanShieldsWorker.cs               # [WorkerTask("scan_shields")]
│   ├── ScanLifeSupportWorker.cs           # [WorkerTask("scan_life_support")]
│   ├── ScanSensorsWorker.cs               # [WorkerTask("scan_sensors")]
│   ├── ScanWeaponsWorker.cs               # [WorkerTask("scan_weapons")]
│   ├── PrioritizeIssuesWorker.cs          # [WorkerTask("prioritize_issues")]
│   ├── NotifyBridgeWorker.cs              # [WorkerTask("notify_bridge")]
│   └── MockAnalysisGenerator.cs           # static helper — verbatim copy from demos/04-dapr/
│
└── Models/
    ├── DiagnosticsInput.cs                # record
    ├── DiagnosticsOutput.cs               # record, BridgeNotification is nullable
    ├── ScanInput.cs                       # record { ShipName, Subsystem }
    ├── ScanResult.cs                      # record { Subsystem, Health, Anomalies[] }
    ├── PrioritizeInput.cs                 # record { ShipName, Scans[] }
    ├── Priority.cs                        # record { Severity, Summary }
    ├── BridgeNotificationInput.cs         # record { ShipName, EngineerName, Priority }
    └── BridgeNotificationResult.cs        # record { Message }
```

**Structural delta vs the Dapr demo:**

```
┌───────────────────────── deltas vs Dapr ─────────────────────────┐
│ + Workflows/DiagnosticsWorkflowRegistrar.cs   (IHostedService)   │
│ + Workers/ (7 files)                          (was Activities/)  │
│ ~ Workflows/DiagnosticsWorkflow.cs                               │
│     was: override RunAsync(WorkflowContext, ...) async/await     │
│     now: static Build() returning a ConductorWorkflow            │
│ - EnterpriseDiagnostics.AppHost/             (no Aspire)         │
│ - EnterpriseDiagnostics.ServiceDefaults/                         │
│ - Valkey container                           (Conductor owns DB) │
│ + docker-compose.yml                         (single Conductor)  │
└──────────────────────────────────────────────────────────────────┘
```

The single most important structural fact for the on-stage narration: **the activity code in `Workers/` looks almost identical to Dapr's `Activities/`** — same `ScanResult` records, same `MockAnalysisGenerator`, same health-rigging. What flipped is the workflow file. In Dapr, it's executable C# with `await`s. In Conductor, it's a *builder* that produces JSON.

## 4. Model shapes (C# records)

Identical record shapes to the Dapr demo, copied verbatim from `demos/04-dapr/EnterpriseDiagnostics.ApiService/Models/DiagnosticsModels.cs` and split one record per file (.NET idiom for non-trivial models). Wire format is snake_case via the system-wide JSON policy in §11.

```csharp
namespace EnterpriseDiagnostics.Conductor.Models;

public record DiagnosticsInput(
    string Id,
    string ShipName,
    string DiagnosticsDate,
    string EngineerName);

public record DiagnosticsOutput(
    string ShipName,
    string DiagnosticsDate,
    string EngineerName,
    Priority Priority,
    string? BridgeNotification);   // nullable — null = green path

public record ScanInput(string ShipName, string Subsystem);
public record ScanResult(string Subsystem, int Health, string[] Anomalies);

public record PrioritizeInput(string ShipName, ScanResult[] Scans);
public record Priority(string Severity, string Summary);

public record BridgeNotificationInput(
    string ShipName,
    string EngineerName,
    Priority Priority);
public record BridgeNotificationResult(string Message);
```

Severity is a plain `string` (not an enum) — matches the Dapr canonical reference and keeps the JSON wire format obvious on stage. Constants live alongside the worker:

```csharp
public static class Severity
{
    public const string Green       = "green";
    public const string YellowAlert = "yellow_alert";
    public const string RedAlert    = "red_alert";
}
```

## 5. Workflow definition (fluent C# builder)

This is the file the audience sees first.

```csharp
using Conductor.Client.Models;
using Conductor.Definition.TaskType;
using Conductor.Definition;

namespace EnterpriseDiagnostics.Conductor.Workflows;

public static class DiagnosticsWorkflow
{
    public const string Name = "diagnostics_workflow";
    public const int Version = 1;

    public static ConductorWorkflow Build()
    {
        // ── Fan-out: 5 parallel subsystem scans ──
        var scanFork = new ForkJoinTask("scan_fork",
            ScanBranch("scan_warp_core",    "warp_core"),
            ScanBranch("scan_shields",      "shields"),
            ScanBranch("scan_life_support", "life_support"),
            ScanBranch("scan_sensors",      "sensors"),
            ScanBranch("scan_weapons",      "weapons"));

        // ── 60s durable WAIT — ⚡ KILL POINT ⚡ ──
        var engineeringReview = new WaitTask("engineering_review")
            .WithDuration(TimeSpan.FromSeconds(60));

        // ── Prioritization ──
        // Pull each scan's output into a single `scans` array. Doing this in the
        // workflow definition keeps the JSON readable on stage (one input
        // parameter, five references). The prioritize_issues worker receives a
        // clean ScanResult[].
        var prioritize = new SimpleTask("prioritize", "prioritize_issues")
            .WithInput("ship_name", "${workflow.input.ship_name}")
            .WithInput("scans", new[]
            {
                "${scan_warp_core.output}",
                "${scan_shields.output}",
                "${scan_life_support.output}",
                "${scan_sensors.output}",
                "${scan_weapons.output}"
            });

        // ── Conditional bridge notification ──
        // value-param evaluator: SWITCH on the `severity` input parameter,
        // which is bound to ${prioritize.output.severity}. Two decision cases
        // (red_alert, yellow_alert) both call notify_bridge; the green path
        // hits the default case (empty), which mirrors `if (severity is "yellow_alert" or "red_alert")`.
        var notifySwitch = new SwitchTask("notify_switch", "value-param")
            .WithExpression("severity")
            .WithInput("severity", "${prioritize.output.severity}")
            .SwitchCase(Severity.RedAlert,    NotifyBranch("notify_red"))
            .SwitchCase(Severity.YellowAlert, NotifyBranch("notify_yellow"));
            // No DefaultCase() → green path is a no-op.

        return new ConductorWorkflow()
            .WithName(Name)
            .WithVersion(Version)
            .WithDescription("USS Enterprise diagnostics — fan-out scans, 60s review, conditional bridge alert")
            .WithOwnerEmail("marc@diagrid.io")
            .WithTask(scanFork)
            .WithTask(engineeringReview)
            .WithTask(prioritize)
            .WithTask(notifySwitch);
    }

    private static SimpleTask ScanBranch(string refName, string subsystem) =>
        new SimpleTask(refName, refName)
            .WithInput("ship_name", "${workflow.input.ship_name}")
            .WithInput("subsystem", subsystem);

    private static SimpleTask NotifyBranch(string refName) =>
        new SimpleTask(refName, "notify_bridge")
            .WithInput("ship_name",      "${workflow.input.ship_name}")
            .WithInput("engineer_name",  "${workflow.input.engineer_name}")
            .WithInput("priority",       "${prioritize.output}");
}
```

> **API-shape note:** the `conductor-csharp` builder methods shown (`WithName`, `WithVersion`, `WithTask`, `WithInput`, `SwitchCase`, etc.) are the *conceptual* surface. The SDK's exact method names have drifted across releases — verify against the pinned NuGet version pre-implementation. If a name differs (e.g. `Name()` vs `WithName()`, or `Input(...)` vs `WithInput(...)`), rename consistently across the file. The structure of the workflow doesn't change.

**Workflow output mapping (green-path null handling).** Conductor lets a workflow definition declare its own output via `outputParameters`. The builder call:

```csharp
return new ConductorWorkflow()
    .WithName(Name)
    .WithVersion(Version)
    // ... tasks above ...
    .WithOutput("ship_name",          "${workflow.input.ship_name}")
    .WithOutput("diagnostics_date",   "${workflow.input.diagnostics_date}")
    .WithOutput("engineer_name",      "${workflow.input.engineer_name}")
    .WithOutput("priority",           "${prioritize.output}")
    // Both notify branches reference the same field; whichever one ran sets it,
    // and on the green path neither task ran so the reference resolves to null.
    .WithOutput("bridge_notification","${notify_red.output.message}");
    // NOTE: if the SDK only supports a single output reference per key, the
    // alternative is to compute bridge_notification in the ASP.NET /status
    // endpoint by inspecting which switch branch fired. Either approach
    // produces the same JSON for the audience.
```

On a `red_alert` run, `${notify_red.output.message}` resolves to the bridge alert string. On a `yellow_alert` run, the `notify_red` ref doesn't exist — depending on Conductor's null-reference semantics, the output is either null or the field is omitted. On `green`, neither ran, so the field is null/absent. **The visible audience contract is: `bridge_notification` is present on red/yellow and null on green.** Verify this empirically with the pinned Conductor version; if the SDK forces a different approach, fall back to computing it in the `/status` handler (also acceptable for a stage demo).

**On-stage payoff for this file:** every C# line here corresponds to a JSON node in the generated workflow definition. Open the Conductor UI → Workflow Definitions → `diagnostics_workflow` → JSON view, and the audience sees:

```jsonc
{
  "name": "diagnostics_workflow",
  "version": 1,
  "tasks": [
    { "name": "scan_fork", "type": "FORK_JOIN", "forkTasks": [ [ {...} ], [ {...} ], ... ] },
    { "name": "engineering_review", "type": "WAIT", "inputParameters": { "duration": "60s" } },
    { "name": "prioritize", "type": "SIMPLE", "taskReferenceName": "prioritize",
      "inputParameters": {
        "ship_name": "${workflow.input.ship_name}",
        "scans": [ "${scan_warp_core.output}", "${scan_shields.output}", ... ]
      } },
    { "name": "notify_switch", "type": "SWITCH",
      "evaluatorType": "value-param",
      "expression": "severity",
      "inputParameters": { "severity": "${prioritize.output.severity}" },
      "decisionCases": {
        "red_alert":    [ { "name": "notify_red",    "taskReferenceName": "notify_red",    "type": "SIMPLE", ... } ],
        "yellow_alert": [ { "name": "notify_yellow", "taskReferenceName": "notify_yellow", "type": "SIMPLE", ... } ]
      } }
  ]
}
```

The narration writes itself: *"This JSON is what the engine sees. My C# code is a builder that produces it. The workers — coming up next — never see this. They see one task at a time, polled off the engine. Workflow-as-data."*

## 6. Worker behavior

Each worker class lives in `Workers/` and exposes a single `[WorkerTask]`-decorated method. The `WorkflowTaskCoordinator` discovers them via the `conductor-csharp` DI extension and starts a poller per task type.

### 6.1 Scan worker example

```csharp
using Conductor.Client.Authentication;
using Conductor.Client.Worker;
using EnterpriseDiagnostics.Conductor.Models;

namespace EnterpriseDiagnostics.Conductor.Workers;

public class ScanWarpCoreWorker
{
    private static readonly string[] Candidates =
    {
        "plasma conduit micro-fracture (deck 36)",
        "warp coil phase variance 0.04%",
        "dilithium articulation frame torque drift",
        "matter/antimatter injector hesitation",
        "magnetic constriction segment 7 thermal creep",
    };

    [WorkerTask(taskType: "scan_warp_core", workerId: "warp_core_worker")]
    public ScanResult Scan(ScanInput input) =>
        MockAnalysisGenerator.Generate(input.Subsystem, Candidates);
}
```

The four other scan workers follow the same shape with a per-subsystem `Candidates` list. **No `Thread.Sleep`** — the worker returns immediately. The 60-second kill window comes from the `WaitTask` in the workflow definition, not from artificial worker latency.

> **Attribute-name note:** depending on the pinned `conductor-csharp` version, the attribute may be `WorkerTask`, `WorkerTaskAttribute`, or wired via `[ConductorWorker]`. Verify pre-implementation and use a single consistent attribute name across all 7 workers.

### 6.2 `MockAnalysisGenerator`

**Identical to the Dapr demo's copy.** Suggested implementation: copy `demos/04-dapr/EnterpriseDiagnostics.ApiService/Activities/MockAnalysisGenerator.cs` verbatim into `Workers/MockAnalysisGenerator.cs` and swap the namespace to `EnterpriseDiagnostics.Conductor.Workers`. The biased-low health range (30..65) and anomaly-sampling logic stay identical.

### 6.3 `PrioritizeIssuesWorker`

Deterministic. Severity from `min(health)` across the scans:

```csharp
public class PrioritizeIssuesWorker
{
    [WorkerTask(taskType: "prioritize_issues", workerId: "prioritize_worker")]
    public Priority Prioritize(PrioritizeInput input)
    {
        var worst = input.Scans.MinBy(s => s.Health)
            ?? throw new InvalidOperationException("scans cannot be empty");

        var severity = worst.Health switch
        {
            < 50 => Severity.RedAlert,
            < 75 => Severity.YellowAlert,
            _    => Severity.Green
        };

        var belowThreshold = input.Scans.Count(s => s.Health < 75);
        var summary = $"{worst.Subsystem} critical at {worst.Health}% health; "
                    + $"{belowThreshold} subsystem(s) below threshold";

        return new Priority(severity, summary);
    }
}
```

### 6.4 `NotifyBridgeWorker`

```csharp
public class NotifyBridgeWorker(ILogger<NotifyBridgeWorker> logger)
{
    [WorkerTask(taskType: "notify_bridge", workerId: "bridge_worker")]
    public BridgeNotificationResult Notify(BridgeNotificationInput input)
    {
        var message = $"🚨 BRIDGE ALERT — USS {input.ShipName} — "
                    + $"{input.Priority.Severity.ToUpper()}: {input.Priority.Summary} "
                    + $"(engineer: {input.EngineerName})";
        logger.LogWarning("{Message}", message);
        return new BridgeNotificationResult(message);
    }
}
```

The `Microsoft.Extensions.Logging.ILogger` is injected via the standard ASP.NET DI container; `conductor-csharp` resolves worker instances through `IServiceProvider`. The 🚨 line lands in the ASP.NET app's stdout — same visible payoff as the Dapr demo.

## 7. HTTP API

A minimal API in `Program.cs` mirroring the Dapr demo's endpoints. Conductor's `WorkflowResourceApi` does the heavy lifting; this layer is thin sugar so the audience uses the same `.http` file across both C# demos.

| Method | Path | Action |
|---|---|---|
| `POST` | `/start` | Reads `DiagnosticsInput` from body. Calls `workflowResourceApi.StartWorkflow(new StartWorkflowRequest { Name = "diagnostics_workflow", Version = 1, WorkflowId = input.Id, Input = ToInputMap(input) })`. Returns `{ workflowId }`. The `workflowId` is the literal value of `input.Id` (`enterprise-ncc-1701`). |
| `GET` | `/status/{id}` | `workflowResourceApi.GetExecutionStatus(id, includeTasks: false)`. Returns `{ status, output? }`. Output is mapped from Conductor's `workflow.output` map back into `DiagnosticsOutput`. |
| `POST` | `/terminate/{id}` | `workflowResourceApi.Terminate(id, reason: "manual termination")`. |

**Sample `enterprise-diagnostics.http`** (byte-for-byte identical to the Dapr demo's, just on port 5050):

```http
### Start diagnostics
POST http://localhost:5050/start
Content-Type: application/json

{
  "id": "enterprise-ncc-1701",
  "ship_name": "USS Enterprise NCC-1701-D",
  "diagnostics_date": "2370-04-08",
  "engineer_name": "Geordi La Forge"
}

### Status
GET http://localhost:5050/status/enterprise-ncc-1701
```

Note the body uses snake_case — that's the wire format Conductor expects in `workflow.input.*` references. The ASP.NET API uses `JsonNamingPolicy.SnakeCaseLower` system-wide so PascalCase records round-trip cleanly (§11).

## 8. Runtime — how to run on stage

```bash
# Terminal 1 — Conductor (server + UI + Elasticsearch in one image)
cd demos/03-conductor && docker compose up

# Wait ~30-60s for Conductor to be healthy at http://localhost:8080/api/health
# UI lands at http://localhost:5000

# Terminal 2 — ASP.NET app (workers + REST API + IHostedService registrar)
cd demos/03-conductor && dotnet run

# Terminal 3 — start a workflow
curl -X POST http://localhost:5050/start \
  -H 'Content-Type: application/json' \
  -d '{"id":"enterprise-ncc-1701","ship_name":"USS Enterprise NCC-1701-D","diagnostics_date":"2370-04-08","engineer_name":"Geordi La Forge"}'
```

Audience watches the **Conductor UI** at `http://localhost:5000` → Workflows → `enterprise-ncc-1701`. The visual progression:

1. Workflow appears in `RUNNING` state with a DAG view.
2. `scan_fork` lights up; the 5 child `scan_*` tasks transition `SCHEDULED → IN_PROGRESS → COMPLETED` near-instantly as the C# workers poll and return.
3. `engineering_review` (WAIT) shows a countdown — Conductor renders the remaining duration.
4. *(Marc kills the `dotnet run` process at ~T+20s into the WAIT)*
5. *(Marc restarts `dotnet run`)* — Conductor keeps the WAIT timer; workers reconnect when the process is back, but the WAIT survives the kill without any worker involvement (it's engine-side).
6. WAIT fires → `prioritize` task scheduled → completes (worker is back) → severity = `red_alert` (rigged).
7. `notify_switch` evaluates → `red_alert` branch → `notify_red` (a `notify_bridge` task) → `🚨 BRIDGE ALERT` line appears in the `dotnet run` stdout.
8. Workflow `COMPLETED`. The DAG view shows the full path; the `Tasks` tab shows every poll, every output, every retry (if any).

**The kill-and-resume payoff is subtly different from the workflow-as-code demos.** In Temporal/Cadence/Dapr/Restate, killing the worker freezes the *workflow*; the engine waits for the worker to come back. In Conductor, the engine *owns the WAIT* — killing the worker only freezes worker-side tasks; the WAIT continues counting down inside Conductor regardless. This is a strength to lean into on stage: *"The engine doesn't care that my workers are dead. The WAIT is happening inside Conductor. When my workers come back, they pick up the next SIMPLE task off the queue. The durable state was never on my side."*

### Why a single Docker image instead of Aspire

The Dapr demo uses Aspire because Dapr's runtime model is "one sidecar per service + a state store". Conductor's runtime model is "one server, workers connect to it" — much simpler topology, no orchestration needed. The single `docker-compose.yml` with one service (`conductor-standalone`) keeps the on-stage start to one command and one URL.

## 9. `docker-compose.yml`

```yaml
services:
  conductor:
    image: conductoross/conductor-standalone:v3.30.0.rc15   # pin pre-talk — RC tag is risky; verify or move to v3.x stable
    ports:
      - "8080:8080"   # Conductor REST API
      - "5000:5000"   # Conductor UI
    environment:
      CONFIG_PROP: config.properties
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/api/health"]
      interval: 10s
      timeout: 5s
      retries: 12
```

> **Pre-talk verify**: `v3.30.0.rc15` is a release candidate tag — confirm it still pulls and runs on the talk laptop a week before the talk. If it's been promoted to GA, prefer the stable tag. Never `:latest`.

## 10. `.csproj` highlights

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="conductor-csharp" Version="3.x.x" />   <!-- pin pre-implementation -->
  </ItemGroup>

</Project>
```

The dependency footprint is deliberately tiny — `conductor-csharp` brings its own HTTP client and JSON layer; no Aspire, no Valkey, no extra orchestration. Compare to the Dapr `.csproj` (Aspire + Valkey component + Dapr.Workflow + Diagrid Dashboard wiring) — this is the *operational* side of the workflow-as-data vs workflow-as-code framing.

## 11. `Program.cs` + `appsettings.json` + `DiagnosticsWorkflowRegistrar`

### `appsettings.json`

```jsonc
{
  "Conductor": {
    "ServerUrl": "http://localhost:8080/api",
    "AuthenticationSettings": null,
    "PollingIntervalMs": 250,
    "DomainSeparator": null
  },
  "Logging": {
    "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" }
  }
}
```

### `Program.cs`

```csharp
using Conductor.Api;
using Conductor.Client;
using Conductor.Client.Extensions;
using EnterpriseDiagnostics.Conductor.Models;
using EnterpriseDiagnostics.Conductor.Workers;
using EnterpriseDiagnostics.Conductor.Workflows;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// snake_case wire format — Conductor's convention; the Dapr demo uses
// the same policy for byte-for-byte JSON compatibility on the .http files.
builder.Services.Configure<JsonOptions>(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    o.SerializerOptions.DictionaryKeyPolicy  = JsonNamingPolicy.SnakeCaseLower;
});

// Conductor client + worker host
var serverUrl = builder.Configuration["Conductor:ServerUrl"]!;
builder.Services.AddSingleton(new Configuration { BasePath = serverUrl });
builder.Services.AddSingleton<WorkflowResourceApi>();
builder.Services.AddSingleton<MetadataResourceApi>();

// Workers — one per task type. The conductor-csharp extension scans the
// assembly for [WorkerTask] methods and starts a poller per task.
builder.Services
    .AddConductorWorkflowTaskCoordinator(builder.Configuration)
    .WithWorker<ScanWarpCoreWorker>()
    .WithWorker<ScanShieldsWorker>()
    .WithWorker<ScanLifeSupportWorker>()
    .WithWorker<ScanSensorsWorker>()
    .WithWorker<ScanWeaponsWorker>()
    .WithWorker<PrioritizeIssuesWorker>()
    .WithWorker<NotifyBridgeWorker>();

// Register the workflow definition idempotently on startup.
builder.Services.AddHostedService<DiagnosticsWorkflowRegistrar>();

var app = builder.Build();

// POST /start — kick off a workflow
app.MapPost("/start", (DiagnosticsInput input, WorkflowResourceApi api) =>
{
    var request = new StartWorkflowRequest
    {
        Name = DiagnosticsWorkflow.Name,
        Version = DiagnosticsWorkflow.Version,
        WorkflowId = input.Id,
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

// GET /status/{id} — describe a workflow
app.MapGet("/status/{id}", (string id, WorkflowResourceApi api) =>
{
    var execution = api.GetExecutionStatus(id, includeTasks: false);
    return Results.Ok(new
    {
        status = execution.Status.ToString(),
        output = execution.Output    // Conductor returns a dictionary — let JSON serializer handle it
    });
});

// POST /terminate/{id}
app.MapPost("/terminate/{id}", (string id, WorkflowResourceApi api) =>
{
    api.Terminate(id, "manual termination");
    return Results.NoContent();
});

app.Run("http://localhost:5050");
```

### `DiagnosticsWorkflowRegistrar.cs`

```csharp
using Conductor.Api;
using Conductor.Client.Models;
using EnterpriseDiagnostics.Conductor.Workflows;

namespace EnterpriseDiagnostics.Conductor.Workflows;

public class DiagnosticsWorkflowRegistrar(
    MetadataResourceApi metadataApi,
    ILogger<DiagnosticsWorkflowRegistrar> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var workflow = DiagnosticsWorkflow.Build();

        // Also register the seven task definitions referenced by the workflow.
        // Conductor requires task defs to exist before a workflow that references
        // them can be registered. RegisterTaskTypes is idempotent (update on conflict).
        var taskTypes = new[]
        {
            "scan_warp_core", "scan_shields", "scan_life_support",
            "scan_sensors", "scan_weapons",
            "prioritize_issues", "notify_bridge"
        };
        var taskDefs = taskTypes.Select(name => new TaskDef
        {
            Name = name,
            RetryCount = 3,
            TimeoutSeconds = 30,
            ResponseTimeoutSeconds = 20,
            OwnerEmail = "marc@diagrid.io"
        }).ToList();

        try
        {
            metadataApi.RegisterTaskDef(taskDefs);
            logger.LogInformation("Registered {Count} task definitions", taskDefs.Count);
        }
        catch (ApiException ex) when (ex.ErrorCode == 409)
        {
            // Already registered — fall through to update path
            foreach (var td in taskDefs) metadataApi.UpdateTaskDef(td);
            logger.LogInformation("Updated {Count} existing task definitions", taskDefs.Count);
        }

        // Register the workflow definition (PUT semantics — replaces same name+version).
        metadataApi.Update(new List<WorkflowDef> { workflow.ToWorkflowDef() });
        logger.LogInformation("Registered workflow '{Name}' v{Version}", workflow.Name, workflow.Version);

        await Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
```

> **Idempotency:** the `MetadataResourceApi.Update(...)` endpoint replaces the workflow definition at the same name+version on each call. Restarting `dotnet run` re-registers cleanly — no manual cleanup between demo runs.

> **Method-name note:** the `Register*` / `Update` / `UpdateTaskDef` method names on `MetadataResourceApi` may differ slightly across `conductor-csharp` versions (some versions use `RegisterWorkflowDef` vs `Update`, `RegisterTaskDef` vs `RegisterTaskDef(List<TaskDef>)`). Verify against the pinned NuGet version and use the actual method names; the *semantics* (register or update) are what matters.

### Why a separate `IHostedService` for registration

Keeping the registrar in its own file means:

1. The workflow definition file (`DiagnosticsWorkflow.cs`) is pure — no Conductor API calls in it. The audience reads it as "the workflow", not "the workflow plus registration plumbing".
2. The registration code is reusable as-is for the `notify_bridge` and `prioritize_issues` task definitions.
3. The `IHostedService` ordering guarantees registration runs before the task coordinator starts polling — workers can't pick up tasks for unknown task definitions.

## 12. plan.md updates

No `plan.md` edits required — the file already reflects this design in §5.3 (Conductor — C# subsection) and §5.0 (idiomatic-naming + deliberate-pairing rationale). This spec is the implementation contract for the work described there.

## 13. Mapping — Dapr (.NET) vs Conductor (.NET) cheat sheet

The talk's headline use of having Conductor and Dapr in the **same host language** is on-stage code-vs-code comparison. Paste into the §5.0 idiomatic-naming slide or the §5.3 transition slide as needed.

| Concern | Dapr (workflow-as-code) | Conductor (workflow-as-data) |
|---|---|---|
| **Workflow file** | `Workflow<TIn, TOut>` subclass, `RunAsync(context, input)` with `await`s | `static ConductorWorkflow Build()` — fluent builder, no execution |
| **Fan-out** | `Task.WhenAll(activityTasks)` | `new ForkJoinTask("scan_fork", ...)` (declarative; engine schedules) |
| **Sleep** | `await context.CreateTimer(TimeSpan.FromSeconds(60))` | `new WaitTask("engineering_review").WithDuration(TimeSpan.FromSeconds(60))` (engine-owned) |
| **Conditional branch** | `if (priority.Severity is "yellow_alert" or "red_alert") { await ... }` | `new SwitchTask(...).SwitchCase("red_alert", ...).SwitchCase("yellow_alert", ...)` |
| **Activity / worker** | `WorkflowActivity<TIn, TOut>` subclass, `RunAsync` | Plain class method decorated `[WorkerTask("task_name")]` |
| **Activity invocation from workflow** | `await context.CallActivityAsync<T>(nameof(Foo), input)` | Workflow definition references task by name (`new SimpleTask("foo_ref", "foo")`); the engine schedules it |
| **Activity registration** | `services.AddDaprWorkflow(o => o.RegisterActivity<Foo>())` | `services.AddConductorWorkflowTaskCoordinator(...).WithWorker<FooWorker>()` (autopolling) |
| **Workflow registration** | `services.AddDaprWorkflow(o => o.RegisterWorkflow<Diagnostics>())` (per-process) | `IHostedService` POSTs the definition to Conductor server at startup (engine-wide) |
| **State storage** | Valkey (state store) referenced from Aspire | Embedded Elasticsearch in `conductor-standalone` (or Postgres + ES in prod) |
| **UI** | Diagrid Dashboard (separate container; OSS Dapr has no native UI) | Built-in Conductor UI on `:5000` |
| **Determinism constraint** | Yes — workflow body must be deterministic for replay | N/A — workers are stateless; engine owns state. **Cannot violate determinism because you cannot run arbitrary code in the workflow** |
| **Killing the worker** | Freezes the workflow until the worker comes back (worker hosts the timer) | Freezes only SIMPLE tasks; WAIT keeps counting in the engine |
| **Wire format** | snake_case via `JsonNamingPolicy.SnakeCaseLower` | snake_case via `JsonNamingPolicy.SnakeCaseLower` *(deliberately matched for the .http file reuse)* |

The most important row for on-stage emphasis is the **determinism row**: *"In Dapr, my workflow is C# code the runtime replays — so I cannot call `DateTime.Now`. In Conductor, my workflow is JSON the engine interprets — so determinism isn't even a question. You trade expressiveness for a stronger guarantee."*

## 14. Out of scope (first pass)

- **Workflow versioning.** Conductor versions definitions natively (the `version: 1` field), but the scenario doesn't require side-by-side versions on stage. Dapr's versioning story is the dimension-2 talking point, not Conductor's.
- **Sub-workflows.** Conductor supports `SUB_WORKFLOW` task type for composition; not needed here.
- **Human task / HTTP task / Inline task.** Out of scope — the scenario uses only SIMPLE + FORK_JOIN + WAIT + SWITCH.
- **JQ expressions in input parameters.** All parameter references in this design use the simpler `${task_ref.output.field}` syntax. JQ is overkill for the scenario.
- **Authentication.** Conductor standalone has no auth; matches the demo posture.
- **Metrics / tracing.** Stage demo only.
- **OAuth / Orkes-hosted Conductor.** OSS standalone is the demo target.

## 15. Risks

| Risk | Mitigation |
|---|---|
| `conductoross/conductor-standalone:v3.30.0.rc15` tag is a release candidate | Verify the tag still pulls and runs on the talk laptop a week pre-talk. If a stable `v3.x.x` GA exists, switch to it. Pin in `docker-compose.yml`. |
| Conductor cold start exceeds 60s (Elasticsearch is heavy) | Start `docker compose up` during the **Dim 4 — Performance** slide (~3 min upstream of the demo). Healthcheck in compose gates readiness so the registrar doesn't try to POST too early. |
| `conductor-csharp` SDK method names drift across NuGet versions (`WithName` vs `Name`, `WorkerTask` vs `ConductorWorker`, `Update` vs `RegisterWorkflowDef`) | Pin the NuGet version in `.csproj`; verify method names against the pinned package's IntelliSense; update this spec inline if any method renames are required during implementation. |
| FORK_JOIN parallelism doesn't show visually fast enough | Workers return instantly (no artificial latency); the Conductor UI's polling interval (~250ms) should make the 5 parallel transitions visible. If the UI batches updates, set the workers' polling intervals lower (`PollingIntervalMs: 100`). |
| SWITCH evaluator-type name (`value-param`) drifts in Conductor server versions | Some Conductor versions use `value-param`, some use `valueParam`. Verify against the pinned server image's docs; the workflow definition will fail validation at registration time if mismatched. |
| Workflow definition references task defs that don't exist → 4xx on registration | The registrar's `RegisterTaskDef(taskDefs)` runs before `Update(workflowDef)` to satisfy the ordering constraint. If a task def registration fails, log loudly and exit non-zero — don't silently start polling on tasks that won't route. |
| Audience asks "why are Conductor and Dapr both in C#?" | Pre-baked talking point from `plan.md` §5.0: *"Same host language, opposite philosophies. Holding the language constant sharpens the workflow-as-data vs workflow-as-code contrast. Cadence + Temporal pull the same trick with Java."* |
| Killing `dotnet run` only freezes worker-side tasks, not the WAIT — audience might not see the durability beat as dramatically | Pre-bake the narration (see §8 "kill-and-resume payoff" paragraph). The Conductor story is actually *stronger* here — the engine survives independently of the workers. Lean into that, don't apologize for it. |
| Conductor UI on `:5000` collides with default ASP.NET dev port | The ASP.NET app explicitly runs on `:5050` (set in `app.Run("http://localhost:5050")`). Don't let `launchSettings.json` override this — delete the file or override `applicationUrl`. |
| JSON wire format mismatch (camelCase vs snake_case) between the C# records and Conductor | `JsonNamingPolicy.SnakeCaseLower` is applied system-wide in `Program.cs`. Verify by checking that `${workflow.input.ship_name}` references resolve correctly in the first end-to-end run. If Conductor receives camelCase, the workflow stalls on the first scan task. |

## 16. Verification checklist (before declaring the demo done)

- [ ] `docker compose up` brings `conductor-standalone` to healthy (`/api/health` returns 200) in <90s from cold pull on the talk laptop.
- [ ] Conductor UI reachable at `http://localhost:5000`; `Workflows` and `Workflow Definitions` panels render.
- [ ] `dotnet run` starts in <10s; `DiagnosticsWorkflowRegistrar` logs "Registered 7 task definitions" and "Registered workflow 'diagnostics_workflow' v1" on first boot, and "Updated 7 existing task definitions" on subsequent boots (idempotency holds).
- [ ] In the Conductor UI, `diagnostics_workflow` v1 appears under Workflow Definitions; opening it shows the JSON form of the workflow.
- [ ] The 7 worker pollers are visible in the Conductor UI's `Task Definitions` / `Workers` view; all 7 show `lastPollTime` updating within the last second.
- [ ] `POST /start` returns `200 OK` with `{ "workflowId": "enterprise-ncc-1701" }`.
- [ ] In the Conductor UI's DAG view, the 5 scan tasks complete near-instantly (no artificial delay), then the `engineering_review` WAIT task appears and counts down.
- [ ] During the 60s WAIT, killing the `dotnet run` process and restarting it does not interrupt the WAIT countdown (engine owns it). After WAIT fires, `prioritize` runs (workers are back) → `notify_red` runs → workflow completes.
- [ ] The `🚨 BRIDGE ALERT` line appears in the `dotnet run` stdout at Warning level with the canonical message format.
- [ ] `GET /status/enterprise-ncc-1701` after completion returns a result with non-null `bridge_notification` (because severity reliably hits `red_alert`).
- [ ] Run the workflow 10× in a row — `bridge_notification` is never null (rigging holds; the SWITCH `red_alert` branch fires every time).
- [ ] Total wall-clock from `docker compose up` cold start to completed workflow: under 5 minutes.
- [ ] **Sister-demo check (vs Dapr):** open `demos/03-conductor/Workflows/DiagnosticsWorkflow.cs` and `demos/04-dapr/EnterpriseDiagnostics.ApiService/Workflows/DiagnosticsWorkflow.cs` side-by-side. The audience can see at a glance that one is a *builder* returning a definition; the other is a *method* with `await`s. The activity/worker code is recognizably similar (`MockAnalysisGenerator` is byte-identical).
- [ ] **JSON reveal check:** in the Conductor UI's Workflow Definition JSON view, every task in the JSON corresponds line-for-line to a `new ForkJoinTask` / `new WaitTask` / `new SimpleTask` / `new SwitchTask` call in `DiagnosticsWorkflow.cs`. The audience can flip between the two and see the correspondence.
