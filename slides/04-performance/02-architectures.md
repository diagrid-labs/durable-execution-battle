---
layout: default
footer: |
  <span>Marc Duiker</span>
  <img src=".demo/assets/diagrid-logo.webp" alt="Diagrid" style="height: 20px; width: auto;">
---

# Architectures at a glance

- **Cadence / Temporal** — sharded 4-service cluster (frontend / history / matching / worker), persistence backend, optional ES.
- **Conductor** — stateless Java server, workers poll for tasks, durable state in Postgres/Redis.
- **Dapr** — workflows ride on the **virtual actor** subsystem in each sidecar; state in your store.
- **Restate** — log-based (Bifrost) + partitioned, Rust core, single binary.
