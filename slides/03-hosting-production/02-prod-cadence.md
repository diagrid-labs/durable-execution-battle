# Production — Cadence

```mermaid
graph TD
  clients[Workers and Clients] --> lb[Load Balancer]
  lb --> fe[Frontend service xN]
  fe --> hist[History service xN - sharded by numHistoryShards]
  fe --> match[Matching service xN - partitioned task lists]
  hist --> match
  iw[Internal Worker service] --> hist
  ring[Ringpop hash ring / membership] --- fe
  ring --- hist
  ring --- match
  hist --> db[(Persistence: Cassandra / SQL)]
  match --> db
  iw --> kafka[Kafka] --> es[(Elasticsearch / OpenSearch)]
  fe --> es
  subgraph regionB[Standby cluster - other region]
    feB[Frontend] --> histB[History] --> dbB[(Persistence)]
  end
  hist -. "XDC async replication (active-passive)" .-> histB
  classDef default stroke:#41bd9b,stroke-width:2px
  linkStyle default stroke:#41bd9b,stroke-width:1px
  style regionB fill:#33271e,stroke:#d9a86b,stroke-width:1px,color:#f0dcc4
```

A horizontally-scaled cluster of four Go services — Frontend (stateless gateway), History (stateful, sharded), Matching (task lists), Internal Worker — in front of a durable store and optional search/log layers. The History service is split into a **fixed, immutable `numHistoryShards`** distributed via a Ringpop hash ring; each shard serializes its workflows, so the shard count is the unit of parallelism and must be over-provisioned up front.

Multi-region HA uses **Cross-DC Replication (XDC)** with Global Domains in **active-passive only** — a domain is ACTIVE in exactly one cluster, with operator-triggered failover and at-least-once activity semantics across the switch.
