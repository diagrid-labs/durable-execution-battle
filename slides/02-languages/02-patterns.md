---
layout: default
footer: |
  <span>Marc Duiker</span>
  <img src=".demo/assets/diagrid-logo.webp" alt="Diagrid" style="height: 20px; width: auto;">
---

# Workflow patterns — same idea, different name

| Pattern | Cadence | Temporal | Conductor | Dapr | Restate |
|---|---|---|---|---|---|
| **Task chaining** | sequential `await` | sequential `await` | `SIMPLE` task chain | `CallActivityAsync` chain | sequential `await` |
| **Fan-out / fan-in** | `Async.function` + `Promise.allOf` | `Async.function` + `Promise.allOf` | `FORK_JOIN` / `FORK_JOIN_DYNAMIC` | `Task.WhenAll` | `Promise.all` |
| **Monitor (loop + sleep)** | `ContinueAsNew` | `ContinueAsNew` | `DO_WHILE` / `START_WORKFLOW` | `ContinueAsNew` | `ctx.sleep` loop |
| **External event** | Signal | Signal / Update | `EVENT` / `HUMAN` system task | `raiseEvent` HTTP API | Awakeable |
| **Child workflow** | `ChildWorkflow` | `ChildWorkflow` | `SUB_WORKFLOW` | `ChildWorkflow` | `ctx.serviceClient` (durable RPC) |
