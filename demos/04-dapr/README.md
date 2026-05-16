# Dapr Workflow — USS Enterprise Diagnostics

.NET 10 + Aspire implementation of the shared `DiagnosticsWorkflow` scenario (see [`demos/shared/scenario.md`](../shared/scenario.md)).

## Status

This directory is a **vendored copy** of [`diagrid-labs/dapr-workflow-versioning/EnterpriseDiagnostics`](https://github.com/diagrid-labs/dapr-workflow-versioning) as of the time it was copied in. The original upstream README is kept at [`README-upstream.md`](./README-upstream.md).

The upstream code does not yet match the shared scenario spec exactly. The deltas tracked for the follow-up adaptation pass are:

| Shared scenario | Upstream code |
|---|---|
| 5 parallel scans: `ScanWarpCore`, `ScanShields`, `ScanLifeSupport`, `ScanSensors`, `ScanWeapons` | 4 LLM-backed activities: `AnalyzeHullActivity`, `AnalyzeWarpCoreActivity`, `AnalyzeSecuritySystemsActivity`, `AnalyzeWeaponSystemsActivity` |
| 60-second `CreateTimer` between fan-in and prioritization (the kill-and-resume window) | No timer |
| `NotifyBridgeActivity` is **conditional** on `priority.severity ∈ {yellow_alert, red_alert}` | `NotifyBridgeActivity` is always invoked |
| `PrioritizeIssues` aggregates the 5 scan results into a deterministic severity | `GenerateRecommendationsActivity` uses an LLM call |
| Activities simulate with `Task.Delay(rand 2–4s)` + canned anomalies based on `shipId % 5` | Activities call the Dapr Conversation API (Anthropic) |

The adaptation will happen in a separate task tracked outside this repo; this directory is the staging ground for that work.

## Running (as-is, today)

See [`README-upstream.md`](./README-upstream.md) — short version:

```shell
aspire run
```

Aspire boots the Valkey state-store container, the ApiService + Dapr sidecar, and the Diagrid Dev Dashboard container. The Aspire dashboard opens automatically.

To start a diagnostics workflow:

```shell
curl -X POST http://localhost:5467/start \
  -H "Content-Type: application/json" \
  -d '{
    "id": "enterprise-ncc-1701",
    "shipName": "USS Enterprise NCC-1701-D",
    "diagnosticsDate": "2370-04-08",
    "engineerName": "Geordi La Forge"
  }'
```

Inspect via the Diagrid Dev Dashboard endpoint exposed in the Aspire dashboard.

## Talk choreography (planned, after the adaptation pass)

See [`plan.md` §5.4](../../plan.md) and [`.demo/04-dapr.demo`](../../.demo/04-dapr.demo) for the on-stage script. Summary:

1. Show the workflow source + 5 activities.
2. `POST /start` — Aspire dashboard shows ApiService + sidecar + Valkey + Diagrid Dashboard healthy.
3. Open the Diagrid Dev Dashboard — 5 parallel `Scan*` activities fan out.
4. Workflow enters the 60-second `CreateTimer`. Kill the ApiService process mid-sleep.
5. Restart the ApiService — the timer durably wakes, prioritization runs, `NotifyBridge` completes (visible in the Diagrid Dev Dashboard activity timeline + the worker stdout banner).
