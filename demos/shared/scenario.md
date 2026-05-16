# Shared demo scenario — USS Enterprise diagnostics

Every framework demo implements **the same workflow shape**, with the same activity names, same fan-out width, same sleep duration, and same kill-point. The implementation idioms differ — the *behavior* doesn't.

## Pseudocode

```
DiagnosticsWorkflow(shipId, missionId):
  # ─── Fan-out: 5 parallel subsystem scans ───
  scans = parallel [
    activity(ScanWarpCore,    shipId),   # ~2-4s simulated
    activity(ScanShields,     shipId),
    activity(ScanLifeSupport, shipId),
    activity(ScanSensors,     shipId),
    activity(ScanWeapons,     shipId)
  ]
  results = await all(scans)             # ── Fan-in ──

  # ─── Prioritization with kill/resume window ───
  log("Engineering review pending — sleeping 60s")
  await sleep(60 seconds)                # ⚡ KILL POINT ⚡
  priority = activity(PrioritizeIssues, results)

  # ─── Conditional bridge notification ───
  if priority.severity in {"yellow_alert", "red_alert"}:
    activity(NotifyBridge, missionId, priority)

  return priority
```

## Activities

| Activity | Returns | Behavior |
|---|---|---|
| `ScanWarpCore` | `{ subsystem: "warp_core", health: 0–100, anomalies: [...] }` | `sleep(rand 2–4s)` then canned anomalies derived from `shipId % 5` |
| `ScanShields` | same shape | same |
| `ScanLifeSupport` | same shape | same |
| `ScanSensors` | same shape | same |
| `ScanWeapons` | same shape | same |
| `PrioritizeIssues` | `{ severity: "green" \| "yellow_alert" \| "red_alert", summary: string }` | Deterministic ranking from the 5 scan results |
| `NotifyBridge` | `void` | Logs a styled `🚨 BRIDGE ALERT` banner to the worker's stdout — **no external HTTP service**. The visible payoff is in the framework's own workflow UI (event history / DAG / journal). |

Rig the scan results so the deterministic ranking lands on `red_alert` — that's what makes `NotifyBridge` fire and lights up the workflow UI on stage.

## Kill-and-resume choreography (identical per framework)

| Step | What | Audience sees |
|---|---|---|
| 1 | `POST /start` (or framework equivalent). Workflow ID `enterprise-ncc-1701`. | Workflow listed as RUNNING in the framework UI. |
| 2 | 5 parallel `Scan*` activities fan out and complete within ~5 s. | 5 activity rows light up. |
| 3 | Workflow enters the 60-second sleep. | A countdown overlay (DemoTime) builds tension. |
| 4 | At T+~20 s, Marc kills the worker process (Ctrl+C or `kill -9`). | "If this were durable, it would survive. Let's see." |
| 5 | Marc restarts the worker. | The sleep durably wakes; `PrioritizeIssues` runs; `NotifyBridge` fires. |
| 6 | Activity timeline updates in the framework UI; worker stdout shows `🚨 BRIDGE ALERT`. | DemoTime overlays a 🚨 graphic. |
| 7 | Cut to the next framework. | — |

## Why no external bridge service

Earlier drafts of this plan included a small HTTP server that received the bridge alert. It's been removed deliberately:

- The bridge notification is a **normal workflow activity**, not an external integration. Keeping it inside the workflow keeps each framework's demo apples-to-apples — no extra moving part on stage, no shared port to coordinate.
- The visible payoff for each framework is **its own workflow UI** showing the activity completing inside the durable history. That UI *is* the wow moment.
- One fewer process to start, one fewer thing to break on a conference Wi-Fi network.
