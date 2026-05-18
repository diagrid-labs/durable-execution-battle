# Conductor (C# / .NET 10) — Enterprise Diagnostics Demo

Stage demo #3 in the **Durable Execution Battle** talk. Workflow shape, severity thresholds, and bridge-message format match the canonical scenario in [`../shared/scenario.md`](../shared/scenario.md). The .NET reference at [`../04-dapr/`](../04-dapr/) is the scenario source of truth; this demo deliberately shares the host language so the **workflow-as-data vs. workflow-as-code** contrast is the only variable on stage.

## Stack

- .NET 10 + ASP.NET Core minimal API
- `conductor-csharp` **1.1.4** (latest stable; no 3.x line exists)
- `conductoross/conductor-standalone:3.15.0` — single Docker image: Conductor server + UI + embedded Elasticsearch
- Newtonsoft.Json for SDK wire serialization (transitively); `System.Text.Json` + `JsonNamingPolicy.SnakeCaseLower` for the ASP.NET endpoints so the `.http` body matches the rest of the talk

## Run on stage (2 terminals)

```bash
# 1) Conductor server (REST :8080, UI on :5000 by default — see "Port collisions" below)
cd demos/03-conductor
docker compose up
# wait ~60s for the container to report `healthy`

# 2) ASP.NET app: workers + REST API + startup registrar in one process
cd demos/03-conductor
dotnet run
```

Open `enterprise-diagnostics.http` (VS Code REST Client) and run **Start the diagnostics workflow**, or:

```bash
curl -X POST http://localhost:5050/start \
  -H 'Content-Type: application/json' \
  -d '{"id":"enterprise-ncc-1701","ship_name":"USS Enterprise NCC-1701-D","diagnostics_date":"2370-04-08","engineer_name":"Geordi La Forge"}'
```

The response is `{"workflow_id":"<uuid>"}`. **Note:** Conductor's `StartWorkflowRequest` has no client-controlled workflow id — the engine assigns a UUID and stores the request's `id` field as the workflow's `correlationId`. Use the returned UUID for `GET /status/{id}` and `POST /terminate/{id}`.

Watch the DAG live in the Conductor UI.

## Port collisions

| Service | Default | Notes |
|---|---|---|
| Conductor REST API | `8080` | Used by `Conductor:ServerUrl` in `appsettings.json` |
| Conductor UI | `5000` | macOS Control Center sometimes binds `5000` (and Node `5001`). If `docker compose up` reports "port already allocated", change the host port in `docker-compose.yml` (e.g. `"5002:5000"`) and open `http://localhost:<new-host-port>` |
| ASP.NET app | `5050` | Deliberately not `5000` so the app and the Conductor UI never collide |

## On-stage choreography

The workflow definition is **JSON the Conductor engine interprets** — built at startup from the fluent C# `DiagnosticsWorkflow.Build()` and POSTed by `DiagnosticsWorkflowRegistrar`.

1. Open `Workflows/DiagnosticsWorkflow.cs`. Walk the audience through the `ForkJoinTask` → `JoinTask` → `WaitTask(~60s)` → `SimpleTask("prioritize_issues")` → `SwitchTask` chain. **There is no `Task.WhenAll` here**, no `await` in workflow code.
2. Open the Conductor UI → **Workflow Definitions** → `diagnostics_workflow` → **JSON** view. Point at the line-for-line correspondence between the fluent C# and the resulting JSON. *"My C# code is a builder. The JSON is the workflow."*
3. `POST /start`. Watch the 5 scan tasks transition `SCHEDULED → IN_PROGRESS → COMPLETED` near-instantly in the DAG view.
4. The workflow enters the `engineering_review` WAIT (~59s — see [WAIT quirk](#wait-quirk-conductor-csharp-114) below). At ~T+20s, `Ctrl+C` the `dotnet run` process. The Conductor UI keeps showing the WAIT counting down — **the engine owns the timer, not the worker**.
5. Wait ~10s. Restart `dotnet run`. After the WAIT fires, `prioritize` runs (workers are back) → `notify_red` runs → 🚨 BRIDGE ALERT line in app stdout → workflow `COMPLETED`.

> **Conductor-specific durability beat:** killing the worker does *not* freeze the workflow — the WAIT is engine-side. The workers come back and pick up the next task off the queue. This is a strength to lean into: the durable state was never on the worker.

## Endpoints

| Method | Path | Notes |
|---|---|---|
| `POST` | `/start` | Starts a workflow. Returns `{ workflow_id }` (snake_case). The request's `id` field becomes the workflow's `correlationId`, not its workflow id. |
| `GET` | `/status/{id}` | Returns `{ status, output }`. `output` includes `priority` (`severity` / `summary`) and a nullable `bridge_notification`. |
| `POST` | `/terminate/{id}` | Forcibly terminates the workflow. |

## Layout

```
demos/03-conductor/
├── docker-compose.yml                         # conductor-standalone (server + UI + ES)
├── EnterpriseDiagnostics.Conductor.csproj     # SDK = conductor-csharp 1.1.4
├── Program.cs                                 # DI + minimal API on :5050
├── appsettings.json / appsettings.Development.json
├── enterprise-diagnostics.http                # REST Client requests
├── SDK_NOTES.md                               # actual symbol bindings for conductor-csharp 1.1.4
│
├── Workflows/
│   ├── DiagnosticsWorkflow.cs                 # fluent C# builder — yields workflow JSON
│   └── DiagnosticsWorkflowRegistrar.cs        # IHostedService — POSTs task defs + workflow def at startup
│
├── Workers/
│   ├── MockAnalysisGenerator.cs               # health range 30..65, biased low — demo rigging
│   ├── WorkerSerialization.cs                 # snake_case bridge between typed records and Conductor's wire dicts
│   ├── ScanWarpCoreWorker.cs                  # typed Scan(ScanInput) + [WorkerTask] adapter
│   ├── ScanShieldsWorker.cs
│   ├── ScanLifeSupportWorker.cs
│   ├── ScanSensorsWorker.cs
│   ├── ScanWeaponsWorker.cs
│   ├── PrioritizeIssuesWorker.cs              # deterministic — unit-tested
│   └── NotifyBridgeWorker.cs                  # writes 🚨 to Console.WriteLine
│
├── Models/                                    # 9 records: inputs, outputs, severity constants
│
└── Tests/                                     # xUnit: severity wire format + deterministic prioritizer
    ├── EnterpriseDiagnostics.Conductor.Tests.csproj
    ├── SeverityJsonTests.cs                   # 5 tests
    └── PrioritizeIssuesWorkerTests.cs         # 8 tests (6 theory + 2 facts)
```

The structural delta vs. the Dapr sister demo is exactly what the §5.3 talking point predicts: `DiagnosticsWorkflow.cs` is a *builder* returning a definition; Dapr's equivalent is a *method* with `await`s. The activity/worker bodies look almost identical between the two.

## Idiomatic-Conductor notes (and SDK 1.1.4 footnotes)

The plan's spec was written speculatively against an imagined "3.x" surface; the actual `conductor-csharp 1.1.4` SDK has several real-world quirks. `SDK_NOTES.md` is the authoritative mapping. Highlights worth knowing on stage:

- **Workflow definition is sent to the engine at startup**, not registered per-process like Dapr. `DiagnosticsWorkflowRegistrar` POSTs the 7 task defs + the workflow def via `MetadataResourceApi.UpdateWorkflowDefinitions(...)`. Idempotent: rerunning `dotnet run` replaces the same name+version.
- **Workers are auto-discovered via assembly scanning.** The SDK's `WorkflowTaskCoordinator.Start()` calls `DiscoverWorkers()`, which scans all loaded assemblies for `[WorkerTask]`-decorated classes and instantiates them via `Activator.CreateInstance`. **That means no DI** — workers have a parameterless ctor that delegates to a `NullLogger<T>` overload. The 🚨 alert uses `Console.WriteLine` because `LogWarning` is swallowed by the null logger.
- **Worker methods use a thin adapter pattern.** A typed `Method(TInput) → TOutput` carries the actual logic (this is what the unit tests call and what the audience reads as "the worker"). A separate `[WorkerTask]`-decorated method takes a `ConductorTask` parameter, deserializes the input dict, calls the typed method, and emits a snake_case-keyed `Dictionary<string,object>`. The bridge lives in `Workers/WorkerSerialization.cs`.
- **Wire format is snake_case end-to-end.** The workflow definition references `${workflow.input.ship_name}` and `${prioritize.output.severity}`; the workers emit those keys via Newtonsoft + `SnakeCaseNamingStrategy`. The ASP.NET endpoints use `System.Text.Json` + `JsonNamingPolicy.SnakeCaseLower`. The `/status` endpoint round-trips through Newtonsoft because `execution.Output`'s values are SDK-owned `JObject`/`JArray` instances System.Text.Json can't introspect.

### WAIT quirk (conductor-csharp 1.1.4)

`WaitTask(string, TimeSpan)`'s ctor reads `duration.Seconds` (the 0..59 component) rather than `duration.TotalSeconds`. `TimeSpan.FromSeconds(60).Seconds` is `0`, which Conductor renders as `"0s"` and the WAIT fires instantly. This demo uses `TimeSpan.FromSeconds(59)` — the largest single-component value — so the on-stage kill window is ~59s rather than exactly 60s. Verified against the SDK source at `~/.nuget/packages/conductor-csharp/1.1.4/src/Conductor/Definition/TaskType/WaitTask.cs:25`.

### FORK_JOIN requires explicit JOIN

Conductor's `ForkJoinTask` system task requires an immediately-following `JoinTask` that names every fork branch's reference name. The fluent C# expresses both, and the engine validates the pairing at workflow registration time. The Dapr equivalent (`Task.WhenAll` over awaited activities) needs no such ceremony — another concrete artifact of the workflow-as-data vs. workflow-as-code framing.

## Tests

```bash
cd demos/03-conductor/Tests && dotnet test
```

13 tests across two files:

- `SeverityJsonTests` — 5 tests confirming PascalCase records serialize to snake_case JSON and that the severity wire strings (`green` / `yellow_alert` / `red_alert`) round-trip cleanly.
- `PrioritizeIssuesWorkerTests` — 8 tests pinning the severity thresholds (`<50 → red_alert`, `<75 → yellow_alert`, else `green`) and the summary message format. These guard the on-stage rigging: if the boundaries drift, the 10-run stress test will surface it.

## Rigging stress test

To confirm the biased-low health range (`30..65`) reliably produces `red_alert` on stage:

```bash
# with the app running:
for i in $(seq 1 10); do
  curl -s -X POST http://localhost:5050/start \
    -H 'Content-Type: application/json' \
    -d "{\"id\":\"rig-$i\",\"ship_name\":\"USS Enterprise NCC-1701-D\",\"diagnostics_date\":\"2370-04-08\",\"engineer_name\":\"Geordi La Forge\"}" | jq -r .workflow_id
done | tee /tmp/rig-ids.txt

# wait ~70s for all 10 to complete (WAITs run in parallel), then:
for id in $(cat /tmp/rig-ids.txt); do
  curl -s http://localhost:5050/status/$id | jq -r '.output.priority.severity'
done | sort | uniq -c
```

Expected: at least 9 of 10 `red_alert`. A typical run yields 9 red / 1 yellow.

## Cleanup

```bash
cd demos/03-conductor && docker compose down -v
```

The `-v` removes Conductor's embedded Elasticsearch volume so the next run starts clean.
