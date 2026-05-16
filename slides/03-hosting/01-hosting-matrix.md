---
layout: default
---

# Hosting matrix

| | Self-host | Storage | Managed cloud | K8s |
|---|---|---|---|---|
| **Cadence** | High | Cassandra/MySQL/Postgres + ES | Instaclustr *(verify)* | Community Helm |
| **Temporal** | High | Cassandra/MySQL/Postgres + ES | Temporal Cloud (+ BYOC AWS GA) | Official Helm |
| **Conductor** | Medium | Postgres/MySQL/Cassandra/Redis + ES/OS | Orkes Cloud | Community |
| **Dapr** | Low — sidecar + state store | Any txn+ETag store | Diagrid Catalyst | Operator / Helm |
| **Restate** | **Lowest — single binary** | Embedded RocksDB; Bifrost replicated log | Restate Cloud | Operator v2.5 |
