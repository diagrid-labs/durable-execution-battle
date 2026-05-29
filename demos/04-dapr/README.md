# Dapr Workflow — USS Enterprise Diagnostics

.NET 10 + Aspire implementation of the shared `DiagnosticsWorkflow` scenario (see [`demos/shared/scenario.md`](../shared/scenario.md)).

## Status

This directory was seeded from [`diagrid-labs/dapr-workflow-versioning/EnterpriseDiagnostics`](https://github.com/diagrid-labs/dapr-workflow-versioning) and has since been adapted to match the shared scenario spec. The original upstream README is kept at [`README-upstream.md`](./README-upstream.md).

### Current workflow shape

```
DiagnosticsWorkflow(shipId, missionId)
  ├─ Fan-out: 5 parallel scans (Task.WhenAll)
  │    ScanWarpCore · ScanShields · ScanLifeSupport · ScanSensors · ScanWeapons
  ├─ context.CreateTimer(60s)            ← ⚡ kill-and-resume window ⚡
  ├─ PrioritizeIssues → { Severity, Summary }
  └─ if Severity ∈ { yellow_alert, red_alert }: NotifyBridge
```

Scan results are biased low (health 30–65) so the aggregate severity reliably lands on `red_alert` for demo drama — the conditional `NotifyBridge` fires every time.

## Running

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

Inspect via the Diagrid Dev Dashboard endpoint exposed in the Aspire dashboard, or:

```shell
curl http://localhost:5467/status/enterprise-ncc-1701
```

## Talk choreography

See [`plan.md` §5.4](../../plan.md) and [`.demo/04-dapr.demo`](../../.demo/04-dapr.demo) for the on-stage script. Summary:

1. Show the workflow source + the 5 `Scan*` activities. Highlight the `Task.WhenAll` fan-out, the `CreateTimer(60s)` sleep, and the conditional `NotifyBridge`.
2. `POST /start` — Aspire dashboard shows ApiService + sidecar + Valkey + Diagrid Dashboard healthy.
3. Open the Diagrid Dev Dashboard — 5 parallel `Scan*` activities fan out and complete within ~1 second.
4. Workflow enters the 60-second `CreateTimer`. Kill the ApiService process mid-sleep (~T+20s).
5. Restart the ApiService — the timer durably wakes, `PrioritizeIssues` runs, `NotifyBridge` fires with the 🚨 BRIDGE ALERT banner (visible in the Diagrid Dev Dashboard activity timeline + the worker stdout).

## Cleanup

`Ctrl+C` the `aspire run` process. Aspire owns the lifecycle of the containers it started (the Valkey state-store and the Diagrid Dev Dashboard) and stops them on graceful shutdown.

Because the containers are Aspire-managed rather than torn down by an explicit `docker compose down`, **verify they actually stopped** — if the shutdown is interrupted they can be orphaned:

```shell
docker ps
```

If a `valkey` or Diagrid Dashboard container is still listed, stop it with `docker stop <id>`.
