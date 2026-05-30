---
layout: default
footer: |
  <span>Marc Duiker</span>
  <img src=".demo/assets/diagrid-logo.webp" alt="Diagrid" style="height: 20px; width: auto;">
---

# Production — Dapr Workflows

```mermaid
graph TD
  subgraph ControlPlane[Dapr Control Plane K8s]
    Operator[Operator]
    Injector[Sidecar Injector]
    Sentry[Sentry mTLS]
    Placement[Placement service]
    Scheduler[Scheduler HA + etcd]
  end
  subgraph DataPlane[Workflow Worker Pods]
    A1[App + daprd engine]
    A2[App + daprd engine]
    A3[App + daprd engine]
  end
  Injector --> A1
  Injector --> A2
  Injector --> A3
  A1 -->|actor lookup| Placement
  A2 -->|actor lookup| Placement
  A3 -->|actor lookup| Placement
  A1 -->|timers/reminders| Scheduler
  A2 -->|timers/reminders| Scheduler
  A3 -->|timers/reminders| Scheduler
  A1 -->|history/state| Store[(External state store)]
  A2 --> Store
  A3 --> Store
  Sentry -.mTLS.-> A1
```

Typically Kubernetes: a control plane (Operator, Sentry for mTLS, Placement, Scheduler) plus the Sidecar Injector. Each worker pod gets a `daprd` sidecar with the embedded `durabletask-go` engine and actor runtime — **there is no dedicated workflow cluster**; the engine scales with your app pods while Placement load-balances the workflow/activity actors. State is event-sourced into an external transactional+ETag store, and timers/reminders live in the Scheduler's etcd.

The **key constraint**: a single workflow instance is a single-threaded actor pinned to one node, so an individual workflow can't be scaled (activities can still fan out). HA is delegated to the chosen state store + Scheduler etcd; there is no built-in cross-region workflow replication — multi-region rides on the datastore's geo-replication.
