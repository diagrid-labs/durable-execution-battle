---
layout: default
footer: |
  <span>Marc Duiker</span>
  <img src=".demo/assets/diagrid-logo.webp" alt="Diagrid" style="height: 20px; width: auto;">
---

# Same idea, different word

| | Cadence | Temporal | Conductor | Dapr | Restate |
|---|---|---|---|---|---|
| *Orchestration unit* | Workflow | Workflow | Workflow | Workflow | Workflow / Service |
| *Side-effect unit* | Activity | Activity | Task | Activity | Handler |
| *User-code process* | Worker | Worker | Worker | App | Service |
| *Tenancy* | Domain | Namespace | Workspace | Namespace (K8s) | Environment¹ |
| *Routing* | Task List | Task Queue | Task Queue | Actor placement² | Partition (key-hashed)² |
| *Durable log* | Event history | Event history | Execution log | Event history (state store) | Journal |
| *Stateful object* | —³ | —³ | — | Actor | Virtual Object |

**Footnotes:**
- ¹ Restate has no tenancy primitive in the OSS server — *Environment* is a **Restate Cloud** concept only
- ² Dapr and Restate have **no pollable task queue** — they push work instead: Dapr hash-distributes workflow/activity *actors* across an app-id's sidecars via the Placement service; Restate hashes keys to *partitions* and pushes each invocation to your registered HTTP deployment
- ³ Temporal/Cadence emulate a stateful object with the long-running **"Entity Workflow"** pattern — a convention, not a primitive (Conductor has no equivalent)
