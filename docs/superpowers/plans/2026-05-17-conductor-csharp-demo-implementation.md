# Conductor (C#) Demo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. **All work happens on `main`. No git commit / push / branch / worktree commands appear in this plan.**

**Goal:** Build the USS Enterprise diagnostics demo as **Conductor with C# / .NET 10** under `demos/03-conductor/`, matching the canonical scenario at `demos/04-dapr/` on activity *behavior* while expressing the workflow as a **fluent C# builder that produces JSON the engine interprets**. The end product runs from a fresh laptop in under 3 minutes (`docker compose up` + `dotnet run`) and demonstrates the workflow-as-data side of the §5.0 deliberate language pairing (Conductor C# vs Dapr C#).

**Architecture:** Single `Microsoft.NET.Sdk.Web` project. One ASP.NET Core minimal API process hosts: (a) seven `[WorkerTask]`-decorated worker methods polled by a `WorkflowTaskCoordinator` running as an `IHostedService`; (b) a `DiagnosticsWorkflowRegistrar` `IHostedService` that idempotently registers the seven task definitions + the workflow definition against Conductor at startup; (c) `POST /start`, `GET /status/{id}`, `POST /terminate/{id}` endpoints. Workflow shape is `ForkJoinTask` (5 scan branches) → `WaitTask(60s)` → `SimpleTask("prioritize_issues")` → `SwitchTask` (red_alert + yellow_alert call `notify_bridge`, green is a no-op). Conductor standalone (server + UI + Elasticsearch) runs in a single Docker container.

**Tech Stack:** .NET 10 · ASP.NET Core minimal API · `conductor-csharp` (Orkes-maintained NuGet, pinned 3.x) · Docker / `conductoross/conductor-standalone:v3.30.0.rc15` · `JsonNamingPolicy.SnakeCaseLower` for wire format · xUnit for the two unit-test surfaces that pay off (deterministic prioritizer + JSON severity round-trip).

**Reference (scenario only, not style):** [`demos/04-dapr/EnterpriseDiagnostics.ApiService/`](../../../demos/04-dapr/EnterpriseDiagnostics.ApiService/). Match the activity names, severity wire strings, subsystem ids, health range, retry posture, bridge message format, and workflow id verbatim. **Do not transliterate Dapr's `Workflow<TIn,TOut>` / `WorkflowActivity<TIn,TOut>` class hierarchy** — Conductor uses plain methods + attributes.

**Design source of truth:** [`docs/superpowers/specs/2026-05-17-conductor-csharp-demo-design.md`](../specs/2026-05-17-conductor-csharp-demo-design.md). When this plan and the spec disagree, the spec wins for *what* and this plan wins for *how/when*.

---

## File Structure

```
demos/03-conductor/
├── README.md                                  # Task 19
├── EnterpriseDiagnostics.Conductor.csproj     # Task 2
├── Program.cs                                 # Task 12
├── appsettings.json                           # Task 13
├── appsettings.Development.json               # Task 13
├── Properties/launchSettings.json             # Task 2
├── docker-compose.yml                         # Task 3
├── enterprise-diagnostics.http                # Task 14
├── .gitignore                                 # Task 2
│
├── Models/
│   ├── DiagnosticsInput.cs                    # Task 4
│   ├── DiagnosticsOutput.cs                   # Task 4
│   ├── ScanInput.cs                           # Task 4
│   ├── ScanResult.cs                          # Task 4
│   ├── PrioritizeInput.cs                     # Task 4
│   ├── Priority.cs                            # Task 4
│   ├── Severity.cs                            # Task 5
│   ├── BridgeNotificationInput.cs             # Task 4
│   └── BridgeNotificationResult.cs            # Task 4
│
├── Workers/
│   ├── MockAnalysisGenerator.cs               # Task 6
│   ├── ScanWarpCoreWorker.cs                  # Task 7
│   ├── ScanShieldsWorker.cs                   # Task 7
│   ├── ScanLifeSupportWorker.cs               # Task 7
│   ├── ScanSensorsWorker.cs                   # Task 7
│   ├── ScanWeaponsWorker.cs                   # Task 7
│   ├── PrioritizeIssuesWorker.cs              # Task 8
│   └── NotifyBridgeWorker.cs                  # Task 9
│
├── Workflows/
│   ├── DiagnosticsWorkflow.cs                 # Task 10
│   └── DiagnosticsWorkflowRegistrar.cs        # Task 11
│
└── Tests/                                     # xUnit project — co-located for stage simplicity
    ├── EnterpriseDiagnostics.Conductor.Tests.csproj  # Task 8
    ├── PrioritizeIssuesWorkerTests.cs                # Task 8
    └── SeverityJsonTests.cs                          # Task 5
```

One responsibility per file. Models split one record per file (matches the Cadence/Temporal sister demos and the `plan.md` §5.3 layout). Tests co-located in a `Tests/` subfolder so the demo is one shippable unit — no separate solution scaffolding needed for stage.

**Structural deltas vs Dapr** (the audience-visible contrast the §5.3 talking point depends on):

```
deltas vs demos/04-dapr/EnterpriseDiagnostics.ApiService/
+ Workflows/DiagnosticsWorkflowRegistrar.cs   (IHostedService — engine-side registration)
+ Workers/ replaces Activities/               (plain methods + [WorkerTask], no base class)
~ Workflows/DiagnosticsWorkflow.cs            (was: RunAsync async/await body)
                                              (now: static Build() returning a ConductorWorkflow)
- AppHost/ and ServiceDefaults/               (no Aspire — Conductor's one-container topology)
- Valkey                                      (Conductor standalone owns state via embedded ES)
+ docker-compose.yml                          (single conductor-standalone service)
```

---

## Task 1: Pin the `conductor-csharp` SDK and lock the API surface

**Goal:** before writing any demo code, identify the exact NuGet version we will pin, and inventory the **literal method/attribute/type names** the rest of this plan assumes (the spec calls these out as needing verification — see §5 and §15 of the spec). Output of this task is a short notes file that subsequent tasks reference verbatim.

**Files:**
- Create: `demos/03-conductor/SDK_NOTES.md`

- [ ] **Step 1: Identify the latest stable `conductor-csharp` version**

Run:
```bash
dotnet new tool-manifest --force >/dev/null 2>&1; \
curl -s 'https://api.nuget.org/v3-flatcontainer/conductor-csharp/index.json' | jq -r '.versions[]' | tail -20
```

Expected: a list of recent versions. Pick the highest stable (non-`-rc`, non-`-preview`) version in the 3.x line. Record it as the pinned version.

- [ ] **Step 2: Create a scratch console project to introspect the SDK**

Run:
```bash
mkdir -p /tmp/conductor-probe && cd /tmp/conductor-probe && \
  dotnet new console -f net10.0 -n Probe -o . >/dev/null && \
  dotnet add package conductor-csharp --version <PINNED_VERSION>
```

Expected: package restores cleanly.

- [ ] **Step 3: Probe the API surface — collect the actual symbol names**

In `/tmp/conductor-probe/Program.cs`, write a smoke program that touches every type the plan needs, and lean on `dotnet build` errors plus IntelliSense to discover the real names. Verify each of these by writing one usage and confirming the project compiles:

| Concept | Names to verify |
|---|---|
| Workflow builder type | `ConductorWorkflow` |
| Workflow-builder fluent methods | `WithName` / `Name`, `WithVersion` / `Version`, `WithTask` / `AddTask`, `WithDescription` / `Description`, `WithOwnerEmail` / `OwnerEmail`, `WithOutput` / `Output` |
| Fan-out task type | `ForkJoinTask` — constructor shape (forkRefName, params SimpleTask[][]) |
| Wait task type | `WaitTask` — `WithDuration(TimeSpan)` or `Duration(...)` or string-only `WithSeconds(int)` |
| Simple task type | `SimpleTask(refName, taskName)`; input setter `WithInput(string,object)` vs `Input(...)` |
| Switch task type | `SwitchTask(refName, evaluatorType)`; case method `SwitchCase` / `Case` / `WithCase`; expression setter `WithExpression` / `Expression` |
| Worker attribute | `WorkerTask` / `WorkerTaskAttribute` / `ConductorWorker` — does it take `taskType:` named arg, `workerId:`? |
| Coordinator registration | `AddConductorWorkflowTaskCoordinator(IConfiguration)`, `.WithWorker<T>()` — fluent chain, or different DI extension? |
| `WorkflowResourceApi` | constructor takes `Configuration` or `ApiClient`? Method `StartWorkflow(StartWorkflowRequest)` returns `string` or `void`? |
| `MetadataResourceApi` | method to register a workflow def: `Update(List<WorkflowDef>)` vs `RegisterWorkflowDef(WorkflowDef)`; same for `TaskDef` |
| Workflow → WorkflowDef conversion | does `ConductorWorkflow` expose `.ToWorkflowDef()` or is registration a different call path? |

- [ ] **Step 4: Record findings in `demos/03-conductor/SDK_NOTES.md`**

Template:

```markdown
# conductor-csharp SDK notes (pinned)

**Package version:** `conductor-csharp <PINNED_VERSION>`
**Verified on:** 2026-05-17

## Type and method bindings

| Plan/spec name              | Actual symbol in this version            |
|-----------------------------|------------------------------------------|
| `ConductorWorkflow.WithName`| `<actual name>`                          |
| `ConductorWorkflow.WithTask`| `<actual name>`                          |
| `ForkJoinTask(refName,...)` | `<constructor sig>`                      |
| `WaitTask.WithDuration`     | `<actual name + sig>`                    |
| `SimpleTask(ref,task)`      | `<constructor sig>`                      |
| `SwitchTask(ref,evaluator)` | `<constructor sig>`                      |
| `SwitchCase(value,task)`    | `<actual name + sig>`                    |
| `[WorkerTask]` attribute    | `<attribute class + ctor args>`          |
| Coordinator DI extension    | `<method name + signature>`              |
| `WorkflowResourceApi.StartWorkflow` | `<method sig + return type>`     |
| `MetadataResourceApi register workflow` | `<method name + sig>`        |
| `MetadataResourceApi register task`     | `<method name + sig>`        |
| Workflow → WorkflowDef converter        | `<method name>`              |

## Wire format defaults

- Does the SDK serialize C# PascalCase property names as `snake_case` by default? (Yes/No)
- If No: where is the override hook? (`JsonNamingPolicy.SnakeCaseLower` in ASP.NET options, or an SDK-specific override?)

## SWITCH evaluator type string

- Verified value: `value-param` / `valueParam` / other → `<actual string>`
```

- [ ] **Step 5: Tear down the scratch project**

```bash
rm -rf /tmp/conductor-probe
```

- [ ] **Step 6: Cross-check the spec**

Open `docs/superpowers/specs/2026-05-17-conductor-csharp-demo-design.md` §5, §6, §11. For every symbol where `SDK_NOTES.md` disagrees with the spec, prefer the SDK_NOTES.md binding in all subsequent tasks. Note the divergence inline in `SDK_NOTES.md` so the spec stays the design document and the notes stay the implementation contract.

---

## Task 2: .NET 10 project scaffold

**Files:**
- Create: `demos/03-conductor/EnterpriseDiagnostics.Conductor.csproj`
- Create: `demos/03-conductor/Program.cs` (placeholder; full body lands in Task 12)
- Create: `demos/03-conductor/Properties/launchSettings.json`
- Create: `demos/03-conductor/.gitignore`

- [ ] **Step 1: Create the `.csproj`**

`demos/03-conductor/EnterpriseDiagnostics.Conductor.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>EnterpriseDiagnostics.Conductor</RootNamespace>
    <AssemblyName>EnterpriseDiagnostics.Conductor</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="conductor-csharp" Version="PINNED_FROM_TASK_1" />
  </ItemGroup>

</Project>
```

Replace `PINNED_FROM_TASK_1` with the version recorded in `SDK_NOTES.md`.

- [ ] **Step 2: Create a placeholder `Program.cs` that starts an empty web host on port 5050**

`demos/03-conductor/Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/", () => "EnterpriseDiagnostics.Conductor — scaffold");
app.Run("http://localhost:5050");
```

- [ ] **Step 3: Create `Properties/launchSettings.json`**

`demos/03-conductor/Properties/launchSettings.json`:

```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5050",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

Port `5050` is deliberate — Conductor's built-in UI takes `5000`. Do not let auto-generated `http` profiles override this.

- [ ] **Step 4: Create `.gitignore`**

`demos/03-conductor/.gitignore`:

```
bin/
obj/
*.user
.vs/
.idea/
.DS_Store
```

- [ ] **Step 5: Restore + build**

Run:
```bash
cd demos/03-conductor && dotnet restore && dotnet build -c Debug --nologo
```

Expected: `Build succeeded`. No errors. Warnings about `conductor-csharp` net-standard target are acceptable.

- [ ] **Step 6: Smoke-run the placeholder host**

Run (in a separate terminal or background):
```bash
cd demos/03-conductor && dotnet run --no-build &
sleep 4
curl -fsS http://localhost:5050/ && echo
kill %1 2>/dev/null
```

Expected: `EnterpriseDiagnostics.Conductor — scaffold`. The host binds to `5050`, not `5000`.

---

## Task 3: docker-compose for `conductor-standalone`

**Files:**
- Create: `demos/03-conductor/docker-compose.yml`

- [ ] **Step 1: Create `docker-compose.yml`**

`demos/03-conductor/docker-compose.yml`:

```yaml
services:
  conductor:
    image: conductoross/conductor-standalone:v3.30.0.rc15
    container_name: conductor-standalone
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

> Note: the `v3.30.0.rc15` tag is a release candidate as recorded in the spec. If a stable `v3.x.x` tag has been promoted by the time this task runs, prefer it. Never `:latest`.

- [ ] **Step 2: Pull the image and bring the stack up**

Run:
```bash
cd demos/03-conductor && docker compose up -d
```

Expected: image pulls (first time only), one container starts.

- [ ] **Step 3: Wait for healthy**

Run:
```bash
for i in {1..18}; do \
  status=$(docker inspect -f '{{.State.Health.Status}}' conductor-standalone 2>/dev/null || echo "starting"); \
  echo "$(date +%T) — $status"; \
  [ "$status" = "healthy" ] && break; \
  sleep 5; \
done
```

Expected: container reaches `healthy` within ~90s. If it doesn't, run `docker compose logs conductor` and address before proceeding.

- [ ] **Step 4: Sanity-check the REST API + UI**

Run:
```bash
curl -fsS http://localhost:8080/api/health && echo
curl -fsS -o /dev/null -w "UI HTTP %{http_code}\n" http://localhost:5000/
```

Expected: health endpoint returns 200 (body shape is server-specific); UI returns 200.

- [ ] **Step 5: Leave the stack running**

Subsequent tasks rely on Conductor being reachable. Bring it down only at the end via `docker compose down -v`.

---

## Task 4: Model records (eight files)

**Files:**
- Create: `demos/03-conductor/Models/DiagnosticsInput.cs`
- Create: `demos/03-conductor/Models/DiagnosticsOutput.cs`
- Create: `demos/03-conductor/Models/ScanInput.cs`
- Create: `demos/03-conductor/Models/ScanResult.cs`
- Create: `demos/03-conductor/Models/PrioritizeInput.cs`
- Create: `demos/03-conductor/Models/Priority.cs`
- Create: `demos/03-conductor/Models/BridgeNotificationInput.cs`
- Create: `demos/03-conductor/Models/BridgeNotificationResult.cs`

All records under namespace `EnterpriseDiagnostics.Conductor.Models`. Public access modifier; one record per file.

- [ ] **Step 1: `DiagnosticsInput.cs`**

```csharp
namespace EnterpriseDiagnostics.Conductor.Models;

public record DiagnosticsInput(
    string Id,
    string ShipName,
    string DiagnosticsDate,
    string EngineerName);
```

- [ ] **Step 2: `DiagnosticsOutput.cs`**

```csharp
namespace EnterpriseDiagnostics.Conductor.Models;

public record DiagnosticsOutput(
    string ShipName,
    string DiagnosticsDate,
    string EngineerName,
    Priority Priority,
    string? BridgeNotification);   // nullable — null = green path
```

- [ ] **Step 3: `ScanInput.cs`**

```csharp
namespace EnterpriseDiagnostics.Conductor.Models;

public record ScanInput(string ShipName, string Subsystem);
```

- [ ] **Step 4: `ScanResult.cs`**

```csharp
namespace EnterpriseDiagnostics.Conductor.Models;

public record ScanResult(string Subsystem, int Health, string[] Anomalies);
```

- [ ] **Step 5: `PrioritizeInput.cs`**

```csharp
namespace EnterpriseDiagnostics.Conductor.Models;

public record PrioritizeInput(string ShipName, ScanResult[] Scans);
```

- [ ] **Step 6: `Priority.cs`**

```csharp
namespace EnterpriseDiagnostics.Conductor.Models;

public record Priority(string Severity, string Summary);
```

- [ ] **Step 7: `BridgeNotificationInput.cs`**

```csharp
namespace EnterpriseDiagnostics.Conductor.Models;

public record BridgeNotificationInput(
    string ShipName,
    string EngineerName,
    Priority Priority);
```

- [ ] **Step 8: `BridgeNotificationResult.cs`**

```csharp
namespace EnterpriseDiagnostics.Conductor.Models;

public record BridgeNotificationResult(string Message);
```

- [ ] **Step 9: Build and confirm**

Run:
```bash
cd demos/03-conductor && dotnet build -c Debug --nologo
```

Expected: `Build succeeded`. No errors.

---

## Task 5: Severity constants + JSON round-trip test

**Files:**
- Create: `demos/03-conductor/Models/Severity.cs`
- Create: `demos/03-conductor/Tests/EnterpriseDiagnostics.Conductor.Tests.csproj`
- Create: `demos/03-conductor/Tests/SeverityJsonTests.cs`

`Severity` is plain strings (not an enum) to match the Dapr canonical wire format. The test gates the wire contract: PascalCase records → `snake_case` JSON → severity stays `red_alert` / `yellow_alert` / `green`.

- [ ] **Step 1: Create `Severity.cs`**

`demos/03-conductor/Models/Severity.cs`:

```csharp
namespace EnterpriseDiagnostics.Conductor.Models;

public static class Severity
{
    public const string Green       = "green";
    public const string YellowAlert = "yellow_alert";
    public const string RedAlert    = "red_alert";
}
```

- [ ] **Step 2: Create the xUnit test project**

`demos/03-conductor/Tests/EnterpriseDiagnostics.Conductor.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <RootNamespace>EnterpriseDiagnostics.Conductor.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\EnterpriseDiagnostics.Conductor.csproj" />
  </ItemGroup>

</Project>
```

> The web project will pick up the test project as a sibling; the test project compiles against the web project for shared models. `IsPackable=false` keeps it out of any future publish.

- [ ] **Step 3: Exclude the `Tests/` folder from the web project's compile set**

Append to `demos/03-conductor/EnterpriseDiagnostics.Conductor.csproj`, just above `</Project>`:

```xml
  <ItemGroup>
    <Compile Remove="Tests/**" />
    <Content Remove="Tests/**" />
    <None Remove="Tests/**" />
  </ItemGroup>
```

This prevents the web SDK from globbing test sources and the test project from being picked up twice.

- [ ] **Step 4: Write the failing test**

`demos/03-conductor/Tests/SeverityJsonTests.cs`:

```csharp
using System.Text.Json;
using EnterpriseDiagnostics.Conductor.Models;
using Xunit;

namespace EnterpriseDiagnostics.Conductor.Tests;

public class SeverityJsonTests
{
    private static JsonSerializerOptions Options() => new()
    {
        PropertyNamingPolicy   = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy    = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Priority_serializes_property_names_as_snake_case_and_keeps_severity_wire_value()
    {
        var priority = new Priority(Severity.RedAlert, "warp_core critical");
        var json = JsonSerializer.Serialize(priority, Options());

        Assert.Contains("\"severity\":\"red_alert\"", json);
        Assert.Contains("\"summary\":\"warp_core critical\"", json);
    }

    [Theory]
    [InlineData("red_alert")]
    [InlineData("yellow_alert")]
    [InlineData("green")]
    public void Priority_round_trips_each_severity(string wire)
    {
        var json = $"{{\"severity\":\"{wire}\",\"summary\":\"x\"}}";
        var parsed = JsonSerializer.Deserialize<Priority>(json, Options())!;

        Assert.Equal(wire, parsed.Severity);
        Assert.Equal("x", parsed.Summary);
    }

    [Fact]
    public void Severity_constants_match_wire_strings()
    {
        Assert.Equal("green",        Severity.Green);
        Assert.Equal("yellow_alert", Severity.YellowAlert);
        Assert.Equal("red_alert",    Severity.RedAlert);
    }
}
```

- [ ] **Step 5: Run the tests — expect green**

Run:
```bash
cd demos/03-conductor/Tests && dotnet test --nologo --verbosity minimal
```

Expected: `Passed!  - Failed: 0, Passed: 5`. If any property serializes as `Severity` instead of `severity` the global JSON options in Task 12 will need fixing — fix here first, in the test's `Options()` helper, since this is the contract every later piece relies on.

---

## Task 6: MockAnalysisGenerator (port verbatim from the Dapr demo)

**Files:**
- Create: `demos/03-conductor/Workers/MockAnalysisGenerator.cs`

- [ ] **Step 1: Copy the canonical implementation**

Source reference: [`demos/04-dapr/EnterpriseDiagnostics.ApiService/Activities/MockAnalysisGenerator.cs`](../../../demos/04-dapr/EnterpriseDiagnostics.ApiService/Activities/MockAnalysisGenerator.cs).

`demos/03-conductor/Workers/MockAnalysisGenerator.cs`:

```csharp
using EnterpriseDiagnostics.Conductor.Models;

namespace EnterpriseDiagnostics.Conductor.Workers;

internal static class MockAnalysisGenerator
{
    public static ScanResult Generate(string subsystem, string[] candidateAnomalies)
    {
        var random = Random.Shared;

        // Biased low so the aggregate severity reliably lands on red_alert for the demo.
        var health = random.Next(30, 66);

        var maxAnomalies = health switch
        {
            >= 60 => 1,
            >= 45 => 2,
            _     => 3
        };

        var anomalyCount = random.Next(1, maxAnomalies + 1);

        var anomalies = candidateAnomalies
            .OrderBy(_ => random.Next())
            .Take(anomalyCount)
            .ToArray();

        return new ScanResult(subsystem, health, anomalies);
    }
}
```

The only difference vs the Dapr file is the namespace (`...Conductor.Workers` instead of `...ApiService.Activities`). The biased-low health range (`30..65`) is the rigging that makes `red_alert` overwhelmingly likely — keep it byte-identical.

- [ ] **Step 2: Build to confirm**

Run:
```bash
cd demos/03-conductor && dotnet build -c Debug --nologo
```

Expected: `Build succeeded`.

---

## Task 7: Five scan workers

**Files:**
- Create: `demos/03-conductor/Workers/ScanWarpCoreWorker.cs`
- Create: `demos/03-conductor/Workers/ScanShieldsWorker.cs`
- Create: `demos/03-conductor/Workers/ScanLifeSupportWorker.cs`
- Create: `demos/03-conductor/Workers/ScanSensorsWorker.cs`
- Create: `demos/03-conductor/Workers/ScanWeaponsWorker.cs`

Each worker is a plain class. One `[WorkerTask]`-decorated method per file. Per-subsystem `Candidates` arrays are copied verbatim from the matching `demos/04-dapr/EnterpriseDiagnostics.ApiService/Activities/Scan*Activity.cs` files so the on-stage text is identical between the two C# demos.

> **Attribute-name reminder:** use the literal attribute name verified in Task 1 / `SDK_NOTES.md`. The plan writes `[WorkerTask(...)]` — substitute whatever the SDK exposes (`[WorkerTaskAttribute]`, `[ConductorWorker]`, etc.).

- [ ] **Step 1: `ScanWarpCoreWorker.cs`**

```csharp
using Conductor.Client.Worker;
using EnterpriseDiagnostics.Conductor.Models;

namespace EnterpriseDiagnostics.Conductor.Workers;

public class ScanWarpCoreWorker(ILogger<ScanWarpCoreWorker> logger)
{
    private static readonly string[] Candidates =
    [
        "Matter/antimatter containment field oscillating outside tolerance",
        "Dilithium crystal alignment drift detected at 0.04 microns",
        "Plasma injector #3 throughput below specification",
        "Warp field coil efficiency reduced by 6% on starboard nacelle",
        "EPS conduit overheating near main reactor manifold"
    ];

    [WorkerTask(taskType: "scan_warp_core", workerId: "warp_core_worker")]
    public ScanResult Scan(ScanInput input)
    {
        logger.LogInformation("ScanWarpCoreWorker: scanning warp core for {ShipName}", input.ShipName);
        return MockAnalysisGenerator.Generate(input.Subsystem, Candidates);
    }
}
```

- [ ] **Step 2: `ScanShieldsWorker.cs`**

```csharp
using Conductor.Client.Worker;
using EnterpriseDiagnostics.Conductor.Models;

namespace EnterpriseDiagnostics.Conductor.Workers;

public class ScanShieldsWorker(ILogger<ScanShieldsWorker> logger)
{
    private static readonly string[] Candidates =
    [
        "Deflector grid phase variance exceeding nominal range",
        "Shield generator 2 recharge rate slower than baseline",
        "Forward shield emitter coupling showing micro-arc events",
        "Harmonic resonance detected in dorsal shield projector",
        "EPS tap to aft shield array running 8% under spec"
    ];

    [WorkerTask(taskType: "scan_shields", workerId: "shields_worker")]
    public ScanResult Scan(ScanInput input)
    {
        logger.LogInformation("ScanShieldsWorker: scanning shields for {ShipName}", input.ShipName);
        return MockAnalysisGenerator.Generate(input.Subsystem, Candidates);
    }
}
```

- [ ] **Step 3: `ScanLifeSupportWorker.cs`**

```csharp
using Conductor.Client.Worker;
using EnterpriseDiagnostics.Conductor.Models;

namespace EnterpriseDiagnostics.Conductor.Workers;

public class ScanLifeSupportWorker(ILogger<ScanLifeSupportWorker> logger)
{
    private static readonly string[] Candidates =
    [
        "Atmospheric CO2 scrubber efficiency reduced by 4.7% on deck 9",
        "Environmental control humidity regulators showing intermittent faults in crew quarters",
        "Oxygen recycler intake filter contamination detected in main air processor",
        "Thermal regulation imbalance reported across deck 12 corridor sections",
        "Gravity plating fluctuation outside nominal range in cargo bay 4"
    ];

    [WorkerTask(taskType: "scan_life_support", workerId: "life_support_worker")]
    public ScanResult Scan(ScanInput input)
    {
        logger.LogInformation("ScanLifeSupportWorker: scanning life support for {ShipName}", input.ShipName);
        return MockAnalysisGenerator.Generate(input.Subsystem, Candidates);
    }
}
```

- [ ] **Step 4: `ScanSensorsWorker.cs`**

```csharp
using Conductor.Client.Worker;
using EnterpriseDiagnostics.Conductor.Models;

namespace EnterpriseDiagnostics.Conductor.Workers;

public class ScanSensorsWorker(ILogger<ScanSensorsWorker> logger)
{
    private static readonly string[] Candidates =
    [
        "Lateral sensor array gain calibration drift detected",
        "Subspace bandwidth allocation below recommended threshold",
        "Long-range sensor pallet 3 reporting elevated noise floor",
        "Navigational deflector return signal showing phase jitter",
        "Tachyon detection grid misaligned by 0.12 degrees"
    ];

    [WorkerTask(taskType: "scan_sensors", workerId: "sensors_worker")]
    public ScanResult Scan(ScanInput input)
    {
        logger.LogInformation("ScanSensorsWorker: scanning sensors for {ShipName}", input.ShipName);
        return MockAnalysisGenerator.Generate(input.Subsystem, Candidates);
    }
}
```

- [ ] **Step 5: `ScanWeaponsWorker.cs`**

```csharp
using Conductor.Client.Worker;
using EnterpriseDiagnostics.Conductor.Models;

namespace EnterpriseDiagnostics.Conductor.Workers;

public class ScanWeaponsWorker(ILogger<ScanWeaponsWorker> logger)
{
    private static readonly string[] Candidates =
    [
        "Phaser array bank 3 power coupling shows intermittent drop",
        "Photon torpedo launcher loading mechanism response time elevated",
        "Targeting sensor calibration drift detected on aft array",
        "Tactical computer subroutine queue depth exceeding threshold",
        "Quantum torpedo magazine humidity outside recommended range"
    ];

    [WorkerTask(taskType: "scan_weapons", workerId: "weapons_worker")]
    public ScanResult Scan(ScanInput input)
    {
        logger.LogInformation("ScanWeaponsWorker: scanning weapons for {ShipName}", input.ShipName);
        return MockAnalysisGenerator.Generate(input.Subsystem, Candidates);
    }
}
```

- [ ] **Step 6: Build**

Run:
```bash
cd demos/03-conductor && dotnet build -c Debug --nologo
```

Expected: `Build succeeded`. If `Conductor.Client.Worker` is the wrong namespace for the `[WorkerTask]` attribute on the pinned version, swap to the one verified in `SDK_NOTES.md` and rerun.

---

## Task 8: PrioritizeIssuesWorker + deterministic unit test

**Files:**
- Create: `demos/03-conductor/Workers/PrioritizeIssuesWorker.cs`
- Create: `demos/03-conductor/Tests/PrioritizeIssuesWorkerTests.cs`

The deterministic prioritizer is unit-tested because its severity thresholds are a cross-demo invariant — if the boundaries drift, the on-stage rigging breaks silently. Worker class itself stays free of randomness so the test is hermetic.

- [ ] **Step 1: Write the failing test**

`demos/03-conductor/Tests/PrioritizeIssuesWorkerTests.cs`:

```csharp
using EnterpriseDiagnostics.Conductor.Models;
using EnterpriseDiagnostics.Conductor.Workers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EnterpriseDiagnostics.Conductor.Tests;

public class PrioritizeIssuesWorkerTests
{
    private static PrioritizeIssuesWorker NewWorker() =>
        new(NullLogger<PrioritizeIssuesWorker>.Instance);

    private static PrioritizeInput InputWithHealths(params int[] healths)
    {
        var scans = healths.Select((h, i) =>
            new ScanResult($"sub_{i}", h, Array.Empty<string>())).ToArray();
        return new PrioritizeInput("USS Test", scans);
    }

    [Theory]
    [InlineData(new[] { 49, 90, 90, 90, 90 }, "red_alert")]
    [InlineData(new[] { 50, 90, 90, 90, 90 }, "yellow_alert")]
    [InlineData(new[] { 74, 90, 90, 90, 90 }, "yellow_alert")]
    [InlineData(new[] { 75, 90, 90, 90, 90 }, "green")]
    [InlineData(new[] { 80, 90, 90, 90, 90 }, "green")]
    [InlineData(new[] { 30, 65, 65, 65, 65 }, "red_alert")]
    public void Severity_is_chosen_from_minimum_health(int[] healths, string expectedSeverity)
    {
        var worker = NewWorker();
        var priority = worker.Prioritize(InputWithHealths(healths));
        Assert.Equal(expectedSeverity, priority.Severity);
    }

    [Fact]
    public void Summary_names_worst_subsystem_and_counts_below_threshold()
    {
        var worker = NewWorker();
        // worst is sub_2 at 30; subs below 75 = sub_0 (40), sub_2 (30), sub_3 (60) = 3
        var input = new PrioritizeInput("USS Test", new[]
        {
            new ScanResult("sub_0", 40, Array.Empty<string>()),
            new ScanResult("sub_1", 80, Array.Empty<string>()),
            new ScanResult("sub_2", 30, Array.Empty<string>()),
            new ScanResult("sub_3", 60, Array.Empty<string>()),
            new ScanResult("sub_4", 90, Array.Empty<string>()),
        });

        var priority = worker.Prioritize(input);

        Assert.Equal("red_alert", priority.Severity);
        Assert.Contains("sub_2", priority.Summary);
        Assert.Contains("30", priority.Summary);
        Assert.Contains("3", priority.Summary);   // 3 of 5 subsystems below threshold
    }

    [Fact]
    public void Throws_when_scans_are_empty()
    {
        var worker = NewWorker();
        Assert.Throws<InvalidOperationException>(() =>
            worker.Prioritize(new PrioritizeInput("USS Test", Array.Empty<ScanResult>())));
    }
}
```

- [ ] **Step 2: Run the test — expect FAIL with "PrioritizeIssuesWorker is not defined"**

Run:
```bash
cd demos/03-conductor/Tests && dotnet test --nologo --verbosity minimal
```

Expected: build errors referencing missing type `PrioritizeIssuesWorker`.

- [ ] **Step 3: Implement `PrioritizeIssuesWorker.cs`**

`demos/03-conductor/Workers/PrioritizeIssuesWorker.cs`:

```csharp
using Conductor.Client.Worker;
using EnterpriseDiagnostics.Conductor.Models;

namespace EnterpriseDiagnostics.Conductor.Workers;

public class PrioritizeIssuesWorker(ILogger<PrioritizeIssuesWorker> logger)
{
    [WorkerTask(taskType: "prioritize_issues", workerId: "prioritize_worker")]
    public Priority Prioritize(PrioritizeInput input)
    {
        logger.LogInformation(
            "PrioritizeIssuesWorker: prioritizing issues for {ShipName}", input.ShipName);

        if (input.Scans.Length == 0)
        {
            throw new InvalidOperationException("scans cannot be empty");
        }

        var worst = input.Scans.OrderBy(s => s.Health).First();
        var belowThreshold = input.Scans.Count(s => s.Health < 75);

        var severity = worst.Health switch
        {
            < 50 => Severity.RedAlert,
            < 75 => Severity.YellowAlert,
            _    => Severity.Green
        };

        var summary =
            $"Worst subsystem: {worst.Subsystem} @ {worst.Health}% health. " +
            $"{belowThreshold} of {input.Scans.Length} subsystems below operational threshold.";

        return new Priority(severity, summary);
    }
}
```

- [ ] **Step 4: Run the tests — expect green**

Run:
```bash
cd demos/03-conductor/Tests && dotnet test --nologo --verbosity minimal
```

Expected: `Passed!  - Failed: 0, Passed: 8` (or higher, depending on theory cases).

---

## Task 9: NotifyBridgeWorker

**Files:**
- Create: `demos/03-conductor/Workers/NotifyBridgeWorker.cs`

The bridge alert message format is a cross-demo invariant — keep it byte-identical to `demos/04-dapr/.../NotifyBridgeActivity.cs`.

- [ ] **Step 1: Create `NotifyBridgeWorker.cs`**

```csharp
using Conductor.Client.Worker;
using EnterpriseDiagnostics.Conductor.Models;

namespace EnterpriseDiagnostics.Conductor.Workers;

public class NotifyBridgeWorker(ILogger<NotifyBridgeWorker> logger)
{
    [WorkerTask(taskType: "notify_bridge", workerId: "bridge_worker")]
    public BridgeNotificationResult Notify(BridgeNotificationInput input)
    {
        var message =
            $"🚨 BRIDGE ALERT — {input.ShipName} — {input.Priority.Severity.ToUpperInvariant()}: " +
            $"{input.Priority.Summary} (engineer: {input.EngineerName})";

        logger.LogWarning("{Message}", message);

        return new BridgeNotificationResult(message);
    }
}
```

- [ ] **Step 2: Build to confirm**

Run:
```bash
cd demos/03-conductor && dotnet build -c Debug --nologo
```

Expected: `Build succeeded`.

---

## Task 10: DiagnosticsWorkflow — fluent C# builder

**Files:**
- Create: `demos/03-conductor/Workflows/DiagnosticsWorkflow.cs`

This is the file the audience reads first on stage. Every line of fluent C# corresponds to a JSON node the engine sees. **Substitute the actual method names from `SDK_NOTES.md`** wherever the example below uses a `With*` name that the SDK actually exposes differently.

- [ ] **Step 1: Create `DiagnosticsWorkflow.cs`**

```csharp
using Conductor.Definition;
using Conductor.Definition.TaskType;
using EnterpriseDiagnostics.Conductor.Models;

namespace EnterpriseDiagnostics.Conductor.Workflows;

public static class DiagnosticsWorkflow
{
    public const string Name    = "diagnostics_workflow";
    public const int    Version = 1;

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

        // ── Prioritization: explicitly assemble the scans array for legibility ──
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

        // ── Conditional bridge notification (SWITCH on severity) ──
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
            .WithTask(notifySwitch)
            .WithOutput("ship_name",           "${workflow.input.ship_name}")
            .WithOutput("diagnostics_date",    "${workflow.input.diagnostics_date}")
            .WithOutput("engineer_name",       "${workflow.input.engineer_name}")
            .WithOutput("priority",            "${prioritize.output}")
            .WithOutput("bridge_notification", "${notify_red.output.message}");
    }

    private static SimpleTask ScanBranch(string refName, string subsystem) =>
        new SimpleTask(refName, refName)
            .WithInput("ship_name", "${workflow.input.ship_name}")
            .WithInput("subsystem", subsystem);

    private static SimpleTask NotifyBranch(string refName) =>
        new SimpleTask(refName, "notify_bridge")
            .WithInput("ship_name",     "${workflow.input.ship_name}")
            .WithInput("engineer_name", "${workflow.input.engineer_name}")
            .WithInput("priority",      "${prioritize.output}");
}
```

- [ ] **Step 2: If the SDK signatures diverge from this draft, fix here**

Open `SDK_NOTES.md` from Task 1. For each row where the actual method name differs from this draft (e.g. `Name()` vs `WithName()`, `Input(...)` vs `WithInput(...)`, `Case(...)` vs `SwitchCase(...)`), rename consistently in this file. The *structure* of the workflow does not change — only the symbol names.

- [ ] **Step 3: Build**

Run:
```bash
cd demos/03-conductor && dotnet build -c Debug --nologo
```

Expected: `Build succeeded`.

- [ ] **Step 4: Quick smoke — instantiate the workflow and dump its JSON**

Add a temporary minimal-API endpoint to `Program.cs` (will be replaced wholesale in Task 12):

```csharp
app.MapGet("/_dump-workflow-def", () =>
{
    var wf = EnterpriseDiagnostics.Conductor.Workflows.DiagnosticsWorkflow.Build();
    return Results.Json(wf);   // or wf.ToWorkflowDef() if available — see SDK_NOTES.md
});
```

Run:
```bash
cd demos/03-conductor && dotnet run --no-build &
sleep 4
curl -fsS http://localhost:5050/_dump-workflow-def | jq '.tasks[].type'
kill %1 2>/dev/null
```

Expected: the four top-level task types print in order — `FORK_JOIN`, `WAIT`, `SIMPLE`, `SWITCH`. If the JSON shape doesn't include a `tasks` array, the SDK serializes the workflow definition differently — inspect the raw output and adjust the registrar in Task 11 accordingly. Then **remove the `/_dump-workflow-def` endpoint** before Task 12 lands.

---

## Task 11: DiagnosticsWorkflowRegistrar (IHostedService)

**Files:**
- Create: `demos/03-conductor/Workflows/DiagnosticsWorkflowRegistrar.cs`

Registers the seven task definitions and the workflow definition at startup. Idempotent — restarting `dotnet run` re-registers cleanly. **Use the actual `MetadataResourceApi` method names from `SDK_NOTES.md`.**

- [ ] **Step 1: Create `DiagnosticsWorkflowRegistrar.cs`**

```csharp
using Conductor.Api;
using Conductor.Client;
using Conductor.Client.Models;

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

    public Task StartAsync(CancellationToken cancellationToken)
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

        var workflow = DiagnosticsWorkflow.Build();
        metadataApi.Update(new List<WorkflowDef> { workflow.ToWorkflowDef() });
        logger.LogInformation(
            "Registered workflow '{Name}' v{Version}",
            DiagnosticsWorkflow.Name, DiagnosticsWorkflow.Version);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

- [ ] **Step 2: Reconcile with `SDK_NOTES.md`**

If the pinned SDK uses different names for any of `RegisterTaskDef`, `UpdateTaskDef`, `Update(List<WorkflowDef>)`, or `ConductorWorkflow.ToWorkflowDef()`, rename here. The *semantics* (register or update) are the contract; the names follow the SDK.

- [ ] **Step 3: Build**

Run:
```bash
cd demos/03-conductor && dotnet build -c Debug --nologo
```

Expected: `Build succeeded`.

---

## Task 12: Program.cs — full DI wiring + minimal API

**Files:**
- Modify: `demos/03-conductor/Program.cs` (replace the placeholder body)

- [ ] **Step 1: Replace `Program.cs` with the full wiring**

```csharp
using Conductor.Api;
using Conductor.Client;
using Conductor.Client.Extensions;
using EnterpriseDiagnostics.Conductor.Models;
using EnterpriseDiagnostics.Conductor.Workers;
using EnterpriseDiagnostics.Conductor.Workflows;
using Microsoft.AspNetCore.Http.Json;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// snake_case on the wire — Conductor expects ${workflow.input.ship_name}-style references.
builder.Services.Configure<JsonOptions>(o =>
{
    o.SerializerOptions.PropertyNamingPolicy   = JsonNamingPolicy.SnakeCaseLower;
    o.SerializerOptions.DictionaryKeyPolicy    = JsonNamingPolicy.SnakeCaseLower;
    o.SerializerOptions.PropertyNameCaseInsensitive = true;
});

// Conductor client + REST API wrappers
var serverUrl = builder.Configuration["Conductor:ServerUrl"]
    ?? throw new InvalidOperationException("Conductor:ServerUrl missing from configuration");
builder.Services.AddSingleton(new Configuration { BasePath = serverUrl });
builder.Services.AddSingleton<WorkflowResourceApi>();
builder.Services.AddSingleton<MetadataResourceApi>();

// Workers — one poller per [WorkerTask] method.
builder.Services
    .AddConductorWorkflowTaskCoordinator(builder.Configuration)
    .WithWorker<ScanWarpCoreWorker>()
    .WithWorker<ScanShieldsWorker>()
    .WithWorker<ScanLifeSupportWorker>()
    .WithWorker<ScanSensorsWorker>()
    .WithWorker<ScanWeaponsWorker>()
    .WithWorker<PrioritizeIssuesWorker>()
    .WithWorker<NotifyBridgeWorker>();

// Idempotently register the seven task defs + the workflow def at startup.
builder.Services.AddHostedService<DiagnosticsWorkflowRegistrar>();

var app = builder.Build();

app.MapPost("/start", (DiagnosticsInput input, WorkflowResourceApi api) =>
{
    var request = new StartWorkflowRequest
    {
        Name       = DiagnosticsWorkflow.Name,
        Version    = DiagnosticsWorkflow.Version,
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

app.MapGet("/status/{id}", (string id, WorkflowResourceApi api) =>
{
    var execution = api.GetExecutionStatus(id, includeTasks: false);
    return Results.Ok(new
    {
        status = execution.Status.ToString(),
        output = execution.Output
    });
});

app.MapPost("/terminate/{id}", (string id, WorkflowResourceApi api) =>
{
    api.Terminate(id, "manual termination");
    return Results.NoContent();
});

app.Run("http://localhost:5050");
```

- [ ] **Step 2: Reconcile method names against `SDK_NOTES.md`**

`AddConductorWorkflowTaskCoordinator`, `.WithWorker<T>()`, `WorkflowResourceApi.StartWorkflow`, and `GetExecutionStatus` may have slightly different names in the pinned SDK version. Replace each with the verified symbol from `SDK_NOTES.md` and rebuild.

- [ ] **Step 3: Build**

Run:
```bash
cd demos/03-conductor && dotnet build -c Debug --nologo
```

Expected: `Build succeeded`. No unresolved references.

---

## Task 13: appsettings.json + appsettings.Development.json

**Files:**
- Create: `demos/03-conductor/appsettings.json`
- Create: `demos/03-conductor/appsettings.Development.json`

- [ ] **Step 1: `appsettings.json`**

```json
{
  "Conductor": {
    "ServerUrl": "http://localhost:8080/api",
    "PollingIntervalMs": 250
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- [ ] **Step 2: `appsettings.Development.json`**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "EnterpriseDiagnostics.Conductor": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

- [ ] **Step 3: Build to confirm**

Run:
```bash
cd demos/03-conductor && dotnet build -c Debug --nologo
```

Expected: `Build succeeded`.

---

## Task 14: `enterprise-diagnostics.http`

**Files:**
- Create: `demos/03-conductor/enterprise-diagnostics.http`

The .http file mirrors the Dapr demo's contract so the audience sees the same JSON body and similar endpoints across both C# demos. The on-stage workflow id is `enterprise-ncc-1701`.

- [ ] **Step 1: Create `enterprise-diagnostics.http`**

```http
@host=http://localhost:5050

### Start the diagnostics workflow
# @name workflowStartRequest
POST {{host}}/start
Content-Type: application/json

{
    "id": "enterprise-ncc-1701",
    "ship_name": "USS Enterprise NCC-1701-D",
    "diagnostics_date": "2370-04-08",
    "engineer_name": "Geordi La Forge"
}

### Get the workflow status
@workflowId={{workflowStartRequest.response.body.$.workflowId}}
GET {{host}}/status/{{workflowId}}

### Get the workflow status by literal id (after a kill+restart)
GET {{host}}/status/enterprise-ncc-1701

### Terminate the workflow
POST {{host}}/terminate/enterprise-ncc-1701
```

The body uses `snake_case` keys because that's the wire format the workflow's `${workflow.input.ship_name}` references expect. The system-wide `JsonNamingPolicy.SnakeCaseLower` in Task 12 makes PascalCase records round-trip cleanly.

---

## Task 15: End-to-end happy-path verification

**Goal:** prove the whole stack works from a cold `dotnet run`. No code changes — purely verification.

- [ ] **Step 1: Ensure Conductor is healthy**

Run:
```bash
docker inspect -f '{{.State.Health.Status}}' conductor-standalone
```

Expected: `healthy`. If not, return to Task 3.

- [ ] **Step 2: Start the app in the foreground (one terminal)**

```bash
cd demos/03-conductor && dotnet run --no-build
```

Expected log lines, in order:
1. `Registered 7 task definitions` *(or `Updated 7 existing task definitions` on re-runs)*
2. `Registered workflow 'diagnostics_workflow' v1`
3. `Now listening on: http://localhost:5050`

Leave it running.

- [ ] **Step 3: Confirm the workflow definition is visible in Conductor UI**

Open `http://localhost:5000` → **Workflow Definitions** → `diagnostics_workflow` (v1). Click into JSON view. Confirm the four top-level tasks: `FORK_JOIN`, `WAIT`, `SIMPLE` (`prioritize`), `SWITCH` (`notify_switch`).

- [ ] **Step 4: Kick off a workflow**

In a second terminal:
```bash
curl -fsS -X POST http://localhost:5050/start \
  -H 'Content-Type: application/json' \
  -d '{"id":"enterprise-ncc-1701","ship_name":"USS Enterprise NCC-1701-D","diagnostics_date":"2370-04-08","engineer_name":"Geordi La Forge"}'
```

Expected: `{"workflowId":"enterprise-ncc-1701"}`.

- [ ] **Step 5: Observe the DAG in the Conductor UI**

Open `http://localhost:5000` → **Workflows** → `enterprise-ncc-1701`. Expect:
1. `scan_fork` runs; the 5 child scan tasks transition `SCHEDULED → IN_PROGRESS → COMPLETED` near-instantly.
2. `engineering_review` (WAIT) appears and starts counting down from 60s.
3. After WAIT fires (or upon refresh after ~60s): `prioritize` runs → severity = `red_alert` (rigged).
4. `notify_switch` evaluates → `red_alert` branch → `notify_red` (a `notify_bridge` task) runs.
5. Workflow state = `COMPLETED`.

- [ ] **Step 6: Confirm the 🚨 line in app stdout**

Switch to the `dotnet run` terminal. Expect a Warning-level log line:

```
🚨 BRIDGE ALERT — USS Enterprise NCC-1701-D — RED_ALERT: Worst subsystem: ... @ NN% health. M of 5 subsystems below operational threshold. (engineer: Geordi La Forge)
```

- [ ] **Step 7: Confirm `/status` returns the canonical output shape**

```bash
curl -fsS http://localhost:5050/status/enterprise-ncc-1701 | jq
```

Expected: JSON with `status: "COMPLETED"` and an `output` object containing non-null `bridge_notification`, a `priority` block with `severity: "red_alert"`, plus the input echoes (`ship_name`, `diagnostics_date`, `engineer_name`).

If the `output` shape is missing fields or shapes them unexpectedly, the workflow definition's `WithOutput(...)` mapping (Task 10) needs adjustment — fix there, restart `dotnet run`, and re-verify.

- [ ] **Step 8: Stop the app cleanly**

In the `dotnet run` terminal: `Ctrl+C`. Expect graceful shutdown logs.

---

## Task 16: Kill-and-resume durability verification

**Goal:** prove the on-stage durability beat actually works: kill the app during the 60s WAIT; restart; the workflow completes.

- [ ] **Step 1: Start a fresh workflow**

Restart the app (`dotnet run --no-build` from `demos/03-conductor`). Kick off a new run with a unique id:

```bash
curl -fsS -X POST http://localhost:5050/start \
  -H 'Content-Type: application/json' \
  -d '{"id":"durability-test-1","ship_name":"USS Enterprise NCC-1701-D","diagnostics_date":"2370-04-08","engineer_name":"Geordi La Forge"}'
```

- [ ] **Step 2: Wait until the WAIT task is active**

Watch the Conductor UI for `durability-test-1`. Wait until the five scan tasks have completed and `engineering_review` is in progress with a visible countdown.

- [ ] **Step 3: Kill the app**

In the `dotnet run` terminal: `Ctrl+C`. Wait for the process to exit fully. The Conductor UI should still show the WAIT counting down (engine-side state).

- [ ] **Step 4: Wait ~30 seconds, then restart the app**

```bash
cd demos/03-conductor && dotnet run --no-build
```

- [ ] **Step 5: Watch the workflow complete**

After WAIT fires, the workers (now back) pick up `prioritize` → `notify_red`. The Conductor UI advances the DAG to `COMPLETED`. The 🚨 line appears in the new stdout.

- [ ] **Step 6: Confirm output**

```bash
curl -fsS http://localhost:5050/status/durability-test-1 | jq '.status, .output.bridge_notification'
```

Expected: `"COMPLETED"` and a non-null bridge notification string. **The kill-and-resume beat is the stage payoff for Conductor specifically — the engine survives the worker dying without losing the WAIT.**

---

## Task 17: Rigging stress test — 10 consecutive runs should all be red_alert

**Goal:** verify the biased-low health range (`30..65`) makes `red_alert` overwhelmingly likely so the on-stage demo doesn't surprise with a yellow or green run.

- [ ] **Step 1: With the app running, fire 10 workflows back-to-back**

```bash
for i in $(seq 1 10); do
  id="rig-$i"
  curl -s -X POST http://localhost:5050/start \
    -H 'Content-Type: application/json' \
    -d "{\"id\":\"$id\",\"ship_name\":\"USS Enterprise NCC-1701-D\",\"diagnostics_date\":\"2370-04-08\",\"engineer_name\":\"Geordi La Forge\"}" > /dev/null
done
```

- [ ] **Step 2: Wait for all to complete (~75s — the WAITs run in parallel)**

Watch the Conductor UI's Workflows list until all 10 show `COMPLETED`.

- [ ] **Step 3: Collect severities**

```bash
for i in $(seq 1 10); do
  curl -s http://localhost:5050/status/rig-$i | jq -r '.output.priority.severity'
done | sort | uniq -c
```

Expected: `10 red_alert` (or 9 red + 1 yellow at the absolute most). If `red_alert` is not at least 9/10, the rigging is off — re-check `MockAnalysisGenerator.Generate` against the Dapr reference; the range must be `random.Next(30, 66)` (upper bound exclusive in .NET, so this generates 30..65 inclusive).

---

## Task 18: Sister-demo + JSON-reveal verification (the stage payoff)

**Goal:** prove the §5.3 talking point lands visually. This is a manual verification — no code.

- [ ] **Step 1: Side-by-side workflow files**

Open these two files side-by-side in the editor:
- `demos/03-conductor/Workflows/DiagnosticsWorkflow.cs`
- `demos/04-dapr/EnterpriseDiagnostics.ApiService/Workflows/DiagnosticsWorkflow.cs`

Confirm at a glance:
- **Conductor:** a static `Build()` method returning a `ConductorWorkflow` — no `async`, no `await`, no execution.
- **Dapr:** a `Workflow<,>` subclass with `RunAsync` doing `Task.WhenAll`, `await context.CreateTimer(...)`, and a conditional `await context.CallActivityAsync(...)`.

This is the visible "workflow-as-data vs workflow-as-code" payoff in the same host language.

- [ ] **Step 2: Side-by-side worker files**

Open:
- `demos/03-conductor/Workers/ScanWarpCoreWorker.cs`
- `demos/04-dapr/.../Activities/ScanWarpCoreActivity.cs`

Confirm: the *worker* code is recognizably similar (same candidate anomalies, same `MockAnalysisGenerator.Generate(...)` call). The structural difference is one is a plain class + `[WorkerTask]` method; the other is a `WorkflowActivity<,>` subclass with `RunAsync`. **This is intentional — the audience should see that the *side-effect* code looks the same; only the workflow file flips philosophy.**

- [ ] **Step 3: JSON reveal**

In a browser, open `http://localhost:5000` → **Workflow Definitions** → `diagnostics_workflow` v1 → JSON view. Confirm the JSON contains:
- A `FORK_JOIN` task named `scan_fork` whose `forkTasks` array contains 5 single-task lists, one per scan.
- A `WAIT` task with a `duration` parameter resolving to 60 seconds.
- A `SIMPLE` task `prioritize` whose `inputParameters.scans` is an array of 5 `${scan_*.output}` references.
- A `SWITCH` task with `evaluatorType: "value-param"` and `decisionCases` keyed by `"red_alert"` and `"yellow_alert"`, both pointing to a `notify_bridge` SIMPLE task.

Flip back to `DiagnosticsWorkflow.cs` and verify each JSON node has a corresponding fluent C# call. **This file-to-JSON correspondence is the on-stage "look — the C# builder IS the JSON" moment.**

---

## Task 19: README — run instructions + stage choreography

**Files:**
- Create: `demos/03-conductor/README.md`

- [ ] **Step 1: Author `README.md`**

```markdown
# Conductor (C# / .NET 10) — USS Enterprise diagnostics

One of the five framework demos for the "Durable Execution Battle" talk
(see [`../../plan.md`](../../plan.md) §5.3). This demo expresses the same
scenario as the canonical Dapr demo at [`../04-dapr/`](../04-dapr/) but
flips the philosophy: the workflow is **JSON the Conductor engine
interprets**, built at startup from a fluent C# `ConductorWorkflow`
builder. The workers — `[WorkerTask]`-decorated methods polled by a
`WorkflowTaskCoordinator` — never see the workflow definition; the
engine schedules them one task at a time.

## Run it

Two terminals.

```bash
# Terminal 1 — Conductor server (server + UI + ES, one image)
cd demos/03-conductor
docker compose up
# wait for "healthy" on `conductor-standalone` (~60s on first pull)
# Conductor REST: http://localhost:8080/api   UI: http://localhost:5000

# Terminal 2 — ASP.NET app (workers + minimal API + IHostedService registrar)
cd demos/03-conductor
dotnet run
# REST endpoints on http://localhost:5050
```

Open `enterprise-diagnostics.http` in VS Code (REST Client extension) and
run the **Start the diagnostics workflow** request, or:

```bash
curl -X POST http://localhost:5050/start \
  -H 'Content-Type: application/json' \
  -d '{"id":"enterprise-ncc-1701","ship_name":"USS Enterprise NCC-1701-D","diagnostics_date":"2370-04-08","engineer_name":"Geordi La Forge"}'
```

Watch the workflow in the Conductor UI at `http://localhost:5000` →
**Workflows** → `enterprise-ncc-1701`.

## Stage choreography

1. Open `Workflows/DiagnosticsWorkflow.cs`. Walk the audience through the
   `ForkJoinTask` → `WaitTask(60s)` → `SimpleTask("prioritize_issues")` →
   `SwitchTask` chain. **No `Task.WhenAll`.** No `await` in workflow code.
2. Open the Conductor UI's **Workflow Definitions** → `diagnostics_workflow`
   → **JSON** view. Point at the line-for-line correspondence with the C#.
   *"My C# code is a builder. The JSON is the workflow."*
3. `POST /start` from `enterprise-diagnostics.http`. Watch the 5 scan
   tasks transition `SCHEDULED → IN_PROGRESS → COMPLETED` near-instantly
   in the DAG view.
4. Workflow enters the 60-second WAIT. At ~T+20s, `Ctrl+C` the
   `dotnet run` process. The WAIT keeps counting down inside Conductor.
5. Wait ~10s. Restart `dotnet run`. After WAIT fires, `prioritize` runs
   (workers are back) → `notify_red` runs → 🚨 BRIDGE ALERT line in stdout
   → workflow `COMPLETED`.

> **Conductor-specific durability beat:** killing the worker does **not**
> freeze the workflow — the WAIT is engine-side. The workers come back and
> pick up the next task off the queue. This is the strength to lean into:
> the durable state was never on the worker.

## Layout

| Path | Purpose |
|---|---|
| `Workflows/DiagnosticsWorkflow.cs`           | Fluent C# builder — produces the workflow JSON |
| `Workflows/DiagnosticsWorkflowRegistrar.cs`  | `IHostedService` — POSTs task defs + workflow def at startup |
| `Workers/*Worker.cs`                         | 7 `[WorkerTask]`-decorated methods (5 scans + prioritize + notify) |
| `Workers/MockAnalysisGenerator.cs`           | Health range 30..65, biased low — rigging for the demo |
| `Models/*.cs`                                | Eight records: inputs, outputs, severity constants |
| `Program.cs`                                 | Minimal API (`POST /start`, `GET /status`, `POST /terminate`) + DI |
| `docker-compose.yml`                         | `conductoross/conductor-standalone` (server + UI + ES) |
| `enterprise-diagnostics.http`                | REST Client requests for stage use |
| `Tests/`                                     | xUnit — deterministic prioritizer + JSON wire format |

## Cleanup

```bash
cd demos/03-conductor && docker compose down -v
```

The `-v` removes Conductor's embedded ES volume, which keeps re-runs clean.
```

- [ ] **Step 2: Spell-check + skim**

Read top-to-bottom for typos, dead links, and any mention of features
that don't exist in the implementation (e.g. don't claim a "pause"
endpoint — the Conductor demo doesn't expose one).

---

## Verification checklist (final gate)

Run through this list once after Task 19 is complete. Tick each box only after you have *observed* the result, not just expected it.

- [ ] `docker compose up` brings `conductor-standalone` to `healthy` in <90s from cold pull.
- [ ] Conductor UI loads at `http://localhost:5000`; both `Workflows` and `Workflow Definitions` panels render.
- [ ] `dotnet run` starts in <10s.
- [ ] On first boot the registrar logs "Registered 7 task definitions" and "Registered workflow 'diagnostics_workflow' v1".
- [ ] On subsequent boots the registrar logs "Updated 7 existing task definitions" (idempotency holds).
- [ ] In the Conductor UI, `diagnostics_workflow` v1 appears under Workflow Definitions; opening it shows the JSON form of the workflow.
- [ ] All 7 task definitions appear under `Task Definitions` with `lastPollTime` updating within the last second.
- [ ] `POST /start` returns `200 OK` with `{ "workflowId": "enterprise-ncc-1701" }`.
- [ ] In the DAG view, the 5 scan tasks complete near-instantly (no artificial latency).
- [ ] `engineering_review` WAIT counts down from 60s.
- [ ] Killing `dotnet run` mid-WAIT does **not** stop the countdown (engine-side).
- [ ] After restarting `dotnet run`, `prioritize` runs, `notify_red` runs, workflow `COMPLETED`.
- [ ] 🚨 BRIDGE ALERT line appears in stdout at Warning level with the canonical message format.
- [ ] `GET /status/enterprise-ncc-1701` returns `bridge_notification` non-null on red_alert runs.
- [ ] 10-run rigging stress test (Task 17) yields ≥ 9 `red_alert` outcomes out of 10.
- [ ] xUnit tests pass: `cd demos/03-conductor/Tests && dotnet test --nologo`.
- [ ] Side-by-side `DiagnosticsWorkflow.cs` (Conductor vs Dapr) shows the workflow-as-data vs workflow-as-code contrast immediately.
- [ ] `Workers/ScanWarpCoreWorker.cs` and `Activities/ScanWarpCoreActivity.cs` (Dapr) share the same candidate anomalies and same `MockAnalysisGenerator.Generate(...)` call — only the framework wrapping differs.
- [ ] `docker compose down -v` cleans up without warnings.
- [ ] Total wall-clock from `docker compose up` cold start to a `COMPLETED` workflow: under 5 minutes.

---

## Out of scope (deferred — see spec §14)

- Workflow versioning side-by-side (Dapr owns that talking point in dimension 2).
- Sub-workflows / HTTP tasks / Inline tasks / JQ expressions.
- Auth (Conductor standalone is unauthenticated; matches stage posture).
- OAuth / Orkes-hosted Conductor.
- Metrics, tracing, OpenTelemetry.
