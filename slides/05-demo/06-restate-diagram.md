# Local dev — Restate

```mermaid
graph LR
  Dev[Developer] -->|POST /start HTTP :3000| Worker
  Dev -->|browser :9070| UI
  Dev --> CLI[Restate CLI]
  CLI -->|deployments register| Proc
  CLI -->|invocations list --all| Proc
  subgraph Box[restate-server single binary]
    Ingress[HTTP Ingress :8080]
    Proc[Partition Processor]
    Rocks[(Embedded RocksDB)]
    Log[Bifrost local log]
    Meta[Raft metadata store]
    UI[Web UI :9070]
    Ingress --> Proc
    Proc --> Rocks
    Proc --> Log
    Proc --> Meta
  end
  Worker["Your SDK worker :9080
   + /start API :3000"]
  Worker -->|workflowSubmit via ingress :8080| Ingress
  Proc -->|bidi HTTP2 invoke| Worker
  Worker -->|journal results| Proc
  classDef default stroke:#41bd9b,stroke-width:2px
  linkStyle default stroke:#41bd9b,stroke-width:1px
  style Box fill:#1e2a38,stroke:#7c93c7,stroke-width:1px,color:#d6def0
```

**One `restate-server` binary** — runtime, embedded RocksDB, durable log, metadata store and Web UI in a single process. Your handlers run as a plain HTTP service. The developer triggers a run with `POST /start` (a thin Express wrapper that forwards to the ingress); the Restate CLI lists invocations straight from the server.
