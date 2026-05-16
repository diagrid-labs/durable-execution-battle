---
layout: default
---

# Bottlenecks at scale

| | Where it hurts first |
|---|---|
| **Cadence / Temporal** | Cassandra ops + Elasticsearch indexing lag |
| **Conductor** | Worker poll overhead + ES index lag |
| **Dapr Workflows** | State-store I/O per checkpoint |
| **Restate** | Disk I/O on RocksDB / Bifrost log replication |

Picking a backing store is a **schedule risk** — Cassandra and ES are operational projects of their own.
