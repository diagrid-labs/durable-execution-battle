---
layout: default
footer: |
  <span>Marc Duiker</span>
  <img src=".demo/assets/diagrid-logo.webp" alt="Diagrid" style="height: 20px; width: auto;">
---

# Same idea, different word

| | Cadence | Temporal | Conductor | Dapr | Restate |
|---|---|---|---|---|---|
| Orchestration unit | Workflow | Workflow | Workflow | Workflow | Workflow / Service |
| Side-effect unit | Activity | Activity | **Task** | Activity | **Handler** |
| User-code process | Worker | Worker | Worker | App (+ sidecar) | Service |
| Tenancy | **Domain** | Namespace | Workspace | Namespace (K8s) | — |
| Routing | **Task List** | Task Queue | Task Queue | — | — |
| Durable log | Event History | Event History | Execution log | — | **Journal** |
| Stateful object | — | — | — | **Actor** | **Virtual Object** |

**False-friend alerts:**
- Conductor **Task** = Temporal/Cadence **Activity** — *not* Temporal's internal "Workflow Task"
- Cadence renamed **Domain → Namespace** and **Task List → Task Queue** at the Temporal fork
- Restate's **Service** is a Handler host; *Virtual Object* = keyed stateful Service; *Workflow* = once-per-key Service
- Only Dapr and Restate ship a first-class stateful-object primitive
