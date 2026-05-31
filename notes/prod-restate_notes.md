# Notes for Production — Restate

The diagram shows the same single Restate binary run as a multi-node cluster, with four colocatable node **roles**, and your handler code running outside the cluster as stateless HTTP services.

Nodes in the diagram:

- **Load Balancer** — spreads incoming traffic across the ingress nodes.
- **Ingress/Admin node (I1, I2)** — the API/ingress surface (and admin role); routes invocations to the partition leaders.
- **Restate cluster (single binary, roles)** — the subgraph; all roles below ship in one binary and can be colocated:
  - **Worker: partition leader / follower (W1, W2)** — each partition has one **leader processor** that tails the log and materializes state; followers stand by for failover.
  - **RocksDB (R1, R2)** — embedded local store where each worker materializes partition state.
  - **Bifrost replicated log (L)** — the **replicated durable log that is the source of truth**; workers append to and tail it.
  - **Log-server (LS1, LS2, LS3)** — store the replicated log segments (quorum replication).
  - **Metadata Raft (M1, M2, M3)** — a Raft group holding cluster metadata, epoch/partition placement (`W1 -.epoch/placement.-> M1`).
- **S3 / MinIO object store** — holds partition **snapshots** for snapshot-bounded recovery.
- **Your SDK services / AWS Lambda** — your handler code runs **outside** the cluster as stateless HTTP services (K8s or Lambda); workers invoke them over bidirectional HTTP/2.

Scaling is key-based sharding — workflow IDs, virtual-object keys and idempotency keys hash to a partition, so all steps/state/timers for a key run in one partition with no hot-path cross-partition coordination. HA comes from quorum replication, **epoch fencing** (no split-brain) and snapshot-bounded recovery; multi-AZ/region is a single stretched cluster via location-aware replication.
