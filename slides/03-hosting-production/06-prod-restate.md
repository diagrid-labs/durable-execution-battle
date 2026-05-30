---
layout: default
footer: |
  <span>Marc Duiker</span>
  <img src=".demo/assets/diagrid-logo.webp" alt="Diagrid" style="height: 20px; width: auto;">
---

# Production — Restate

```mermaid
graph TD
  LB[Load Balancer] --> I1[Ingress/Admin node]
  LB --> I2[Ingress/Admin node]
  subgraph Cluster[Restate cluster - single binary, roles]
    I1 --> W1[Worker: partition leader]
    I2 --> W2[Worker: partition leader/follower]
    W1 --> R1[(RocksDB)]
    W2 --> R2[(RocksDB)]
    W1 --> L[Bifrost replicated log]
    W2 --> L
    L --> LS1[Log-server]
    L --> LS2[Log-server]
    L --> LS3[Log-server]
    M1[Metadata Raft] --- M2[Metadata Raft]
    M2 --- M3[Metadata Raft]
    W1 -.epoch/placement.-> M1
  end
  W1 -->|snapshots| OS[(S3 / MinIO object store)]
  W2 -->|snapshots| OS
  W1 -->|bidi HTTP2 invoke| SVC[Your SDK services / AWS Lambda]
  W2 -->|invoke| SVC
```

The same single binary run as a multi-node cluster with four colocatable node **roles**: worker, log-server, metadata-server, admin/ingress. The replicated durable log (**Bifrost**) is the source of truth; each partition has one leader processor tailing the log and materializing state in embedded RocksDB, with object storage (S3/MinIO) holding snapshots. Your handler code runs **outside** the cluster as stateless HTTP services on K8s or AWS Lambda.

Scaling is key-based sharding — workflow IDs, virtual-object keys and idempotency keys hash to a partition, so all steps/state/timers for a key run in one partition with no hot-path cross-partition coordination. HA comes from quorum replication, epoch fencing (no split-brain) and snapshot-bounded recovery; multi-AZ/region is a single stretched cluster via location-aware replication.
