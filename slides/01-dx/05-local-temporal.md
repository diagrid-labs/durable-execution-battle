---
layout: default
footer: |
  <span>Marc Duiker</span>
  <img src=".demo/assets/diagrid-logo.webp" alt="Diagrid" style="height: 20px; width: auto;">
---

# Local dev — Temporal

```mermaid
graph TD
  Dev[Developer]
  CLI[temporal CLI]
  subgraph DevServer[temporal server start-dev single process]
    SRV["Embedded Temporal Server<br/>Frontend+History+Matching+Worker"]
    DB[("SQLite<br/>in-memory or file")]
    UI[Embedded Web UI :8233]
    SRV --- DB
    SRV --- UI
  end
  Worker["Your Worker process<br/>app code + SDK"]
  CLI -->|start/inspect workflows :7233| SRV
  Worker -->|poll task queues gRPC :7233| SRV
  Dev --> CLI
  Dev --> Worker
  Dev -->|browser| UI
```

**`temporal server start-dev`** — one binary bundles the server, SQLite persistence and the Web UI. Zero external dependencies; you only add your worker process.
