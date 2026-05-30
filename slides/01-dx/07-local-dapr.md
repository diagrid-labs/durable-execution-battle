---
layout: default
footer: |
  <span>Marc Duiker</span>
  <img src=".demo/assets/diagrid-logo.webp" alt="Diagrid" style="height: 20px; width: auto;">
---

# Local dev — Dapr Workflows

```mermaid
graph LR
  Dev[Developer] -->|dapr init| CLI[Dapr CLI]
  CLI -->|dapr run| App
  subgraph LocalMachine
    App[Worker App + Dapr SDK] -->|gRPC work-item stream| Sidecar[Dapr sidecar
     with wf engine]
    Sidecar -->|history/inbox/state| Store[(Redis state store)]
    Sidecar -->|actor placement| Placement[Placement service]
    Sidecar -->|timers/reminders| Scheduler[Scheduler service + embedded etcd]
  end
```

**`dapr init` + `dapr run`** — no separate workflow server. The engine is embedded in the `daprd` sidecar; durability rides on a state store, Placement and Scheduler.
