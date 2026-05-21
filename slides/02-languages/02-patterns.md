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
| **Monitor (loop + sleep)** | `ContinueAsNew` | `ContinueAsNew` | `DO_WHILE` / `START_WORKFLOW` | `ContinueAsNew` | `ctx.sleep` loop *(no CAN)* |
| **External event / human-in-the-loop** | Signal | Signal / Update | `EVENT` / `HUMAN` system task | `raiseEvent` HTTP API | Awakeable |
| **Child workflow** | `ChildWorkflow` | `ChildWorkflow` | `SUB_WORKFLOW` | `ChildWorkflow` | `ctx.serviceClient` (durable RPC) |
| **Cross-language orchestration** | — *(Go + Java only)* | Nexus (v1.31 GA) | native — workers in any language | HTTP API — start/signal from anywhere | native — handlers are HTTP |
| **Versioning** *(not a pattern, but worth comparing)* | `workflow.GetVersion` | Worker Deployment Versioning (GA) + patching | integer DSL version per workflow | `IsPatched` + named workflow versions *(since 1.17)* | service revisions on redeploy |

**Two asymmetries to keep in mind:**

- **Restate** has no `ContinueAsNew` and no separate "child workflow" primitive — both fold into the same `ctx.run` + durable RPC model.
- **Cadence** is the only framework here without a credible cross-language story in 2026 — practically a Go + Java product.
