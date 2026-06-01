# Production — Temporal

```mermaid
graph TD
  Clients["App clients + Worker fleet<br/>SDK over gRPC"]
  LB[Load balancer]
  Clients --> LB
  subgraph Cluster[Temporal Service - Ringpop membership ring]
    FE["Frontend Service<br/>stateless gateway"]
    HIST["History Service<br/>sharded stateful"]
    MATCH["Matching Service<br/>task queues"]
    IWRK["Internal Worker Service<br/>system workflows"]
    FE --> HIST
    FE --> MATCH
    HIST <--> MATCH
  end
  LB --> FE
  HIST --> DB[("Persistence<br/>Cassandra / MySQL / PostgreSQL")]
  MATCH --> DB
  IWRK --> DB
  FE --> VIS[("Visibility<br/>Elasticsearch / OpenSearch or SQL")]
  HIST --> VIS
  subgraph DR[Multi-cluster replication]
    mirror2["Full mirror: Frontend / History / Matching / Worker"] --> DB2[(Standby cluster DB)]
  end
  HIST -.async replication.-> mirror2
  classDef default stroke:#41bd9b,stroke-width:2px
  linkStyle default stroke:#41bd9b,stroke-width:1px
  style Cluster fill:#1e2a38,stroke:#7c93c7,stroke-width:1px,color:#d6def0
  style DR fill:#33271e,stroke:#d9a86b,stroke-width:1px,color:#f0dcc4
```

The same four Go services as Cadence (Frontend, History, Matching, internal Worker) discovering each other over Ringpop, fronting a clustered DB (Cassandra / MySQL 8+ / PostgreSQL 12+). Visibility lands in the same SQL store (standard) or Elasticsearch/OpenSearch (advanced). The **History Shard count is the master, immutable scaling parameter** (≈1 History instance per ~500 shards); in practice the persistence DB and the ES cluster are the real bottleneck.

HA baseline is multiple replicas + clustered DB, with multi-cluster async replication (active→passive). **Temporal Cloud HA namespaces** add same-region / multi-region / multi-cloud (AWS↔GCP) active-standby with automatic failover (~20-min RTO, sub-1-min RPO, 99.99% SLA).
