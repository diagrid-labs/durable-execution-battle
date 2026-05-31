---
layout: default
footer: |
  <span>Marc Duiker</span>
  <img src=".demo/assets/diagrid-logo.webp" alt="Diagrid" style="height: 20px; width: auto;">
---

# Self Hosting

| | Self-host effort | Storage | K8s |
|---|---|---|---|
| **Cadence** | High | Cassandra/MySQL/Postgres + ES | Community Helm |
| **Temporal** | High | Cassandra/MySQL/Postgres + ES | Official Helm |
| **Conductor** | Medium | Postgres/MySQL/Cassandra/Redis + ES/OS | Community |
| **Dapr** | Low — sidecar + state store | Any txn+ETag store | Operator / Helm |
| **Restate** | Lowest — single binary | Embedded RocksDB; Bifrost replicated log | Operator v2.5 |
