# Local dev — Cadence

```mermaid
graph TD
  dev[Developer]
  dev -->|POST /start HTTP :8080| worker
  dev -->|list / describe wf| cli
  dev -->|observe runs| ui

  worker[Your Worker process Go/Java SDK - hosts HTTP API :8080] -->|start wf + long-poll tasks| fe

  subgraph server[cadence-server auto-setup container]
    cli[Cadence CLI] --> fe
    fe[Frontend] --> hist[History]
    fe --> match[Matching]
    hist --> match
    iw[Internal Worker]
  end

  subgraph web[cadence-web container]
    ui[Cadence Web UI :8088]
  end
  ui -->|gRPC| fe

  subgraph cass[cadence-cassandra container]
    db[(Cassandra :9042)]
  end
  server --> db

  classDef default stroke:#41bd9b,stroke-width:2px
  linkStyle default stroke:#41bd9b,stroke-width:1px
  style server fill:#1e2a38,stroke:#7c93c7,stroke-width:1px,color:#d6def0
  style web fill:#1e2a38,stroke:#7c93c7,stroke-width:1px,color:#d6def0
  style cass fill:#1e2a38,stroke:#7c93c7,stroke-width:1px,color:#d6def0
```

**`docker compose up`** — three containers: `cadence-server` (auto-setup: Frontend, History, Matching + Internal Worker, with the CLI bundled in), `cadence-web` (Web UI :8088), and `cadence-cassandra` (persistence). You run your own Worker process, which hosts an HTTP API on :8080 — a `POST /start` kicks off the workflow via the SDK client.
