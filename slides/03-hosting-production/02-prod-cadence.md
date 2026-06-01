# Production — Cadence

```mermaid
graph TD
  clients[Workers and Clients] --> lb[Load Balancer]
  lb --> fe
  subgraph Cluster[Cadence cluster - Ringpop hash ring / membership]
    fe["Frontend service xN<br/>stateless gateway"]
    hist["History service xN<br/>sharded by numHistoryShards"]
    match["Matching service xN<br/>partitioned task lists"]
    iw["Internal Worker service<br/>system workflows"]
    fe --> hist
    fe --> match
    hist --> match
    iw --> hist
  end
  hist --> db[(Persistence: Cassandra / SQL)]
  match --> db
  iw --> kafka[Kafka] --> es[(Elasticsearch / OpenSearch)]
  fe --> es
  subgraph regionB[Standby cluster - other region]
    mirrorB["Full mirror: FE / History / Matching / Worker"] --> dbB[(Persistence)]
  end
  hist -. "XDC async replication (active-passive)" .-> mirrorB
  classDef default stroke:#41bd9b,stroke-width:2px
  linkStyle default stroke:#41bd9b,stroke-width:1px
  style Cluster fill:#1e2a38,stroke:#7c93c7,stroke-width:1px,color:#d6def0
  style regionB fill:#33271e,stroke:#d9a86b,stroke-width:1px,color:#f0dcc4
```

A horizontally-scaled cluster of four Go services — Frontend (stateless gateway), History (stateful, sharded), Matching (task lists), Internal Worker — in front of a durable store and optional search/log layers. The History service is split into a **fixed, immutable `numHistoryShards`** distributed via a Ringpop hash ring; each shard serializes its workflows, so the shard count is the unit of parallelism and must be over-provisioned up front.

Multi-region HA uses **Cross-DC Replication (XDC)** with Global Domains in **active-passive only** — a domain is ACTIVE in exactly one cluster, with operator-triggered failover and at-least-once activity semantics across the switch.
