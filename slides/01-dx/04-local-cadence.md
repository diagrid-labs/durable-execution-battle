---
layout: default
footer: |
  <span>Marc Duiker</span>
  <img src=".demo/assets/diagrid-logo.webp" alt="Diagrid" style="height: 20px; width: auto;">
---

# Local dev — Cadence

```mermaid
graph TD
  dev[Developer] -->|register domain, start wf| cli[cadence CLI]
  dev --> ui[Cadence Web UI :8088]
  cli --> fe
  ui --> fe
  subgraph server[Cadence auto-setup container]
    fe[Frontend] --> hist[History]
    fe --> match[Matching]
    hist --> match
    iw[Internal Worker]
  end
  worker[Your Worker process Go/Java SDK] -->|long-poll tasks| fe
  server --> db[(Persistence DB: Cassandra / MySQL / Postgres)]
```

**`docker-compose up`** — auto-setup server (4 roles in one container) + a persistence DB + Web UI. You run your own worker and register a domain via the CLI.
