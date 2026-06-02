# Local dev — Dapr Workflows

```mermaid
graph LR
  Dev[Developer] -->|aspire run| AspireManaged
  Dev -->|POST /start HTTP :5467| App
  Dev -->|dapr workflow list -a wf-app| CLI[Dapr CLI]
  Dev -->|inspect workflows| Dashboard
  subgraph AspireManaged[Managed by Aspire]
    App[Worker App + Dapr SDK
     hosts HTTP API :5467] -->|gRPC work-item stream| Sidecar[Dapr sidecar
     with wf engine]
    Sidecar -->|history/inbox/state| Store[(Valkey state store :16379)]
    Dashboard[Diagrid Dev Dashboard] -->|retrieve workflow data| Store
  end
  Sidecar -->|actor placement| Placement[Placement service]
  Sidecar -->|timers/reminders| Scheduler[Scheduler service]
  CLI -->|retrieve workflow data| Store
  classDef default stroke:#41bd9b,stroke-width:2px
  linkStyle default stroke:#41bd9b,stroke-width:1px
  style AspireManaged fill:#1e2a38,stroke:#7c93c7,stroke-width:1px,color:#d6def0
```

**`aspire run`** — Aspire owns the lifecycle of the Worker App, its Dapr sidecar and the Valkey state store. No separate workflow server: the engine is embedded in the `daprd` sidecar and durability rides on the state store. The developer triggers a run with `POST /start`; the Dapr CLI reads workflow data from the state store to list instances.
