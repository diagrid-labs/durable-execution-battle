# Notes for Production — Temporal

The diagram is the same four-service shape as Cadence (Temporal is the Cadence fork), wrapped in a Ringpop membership ring, fronting a clustered DB and a visibility store, with a standby cluster for DR.

Nodes in the diagram:

- **App clients + Worker fleet (SDK over gRPC)** — your application clients and worker processes; they reach the cluster over gRPC via the load balancer.
- **Load balancer** — spreads traffic across the stateless Frontend instances.
- **Temporal Service (Ringpop membership ring)** — the subgraph holding the four core services that discover each other over Ringpop:
  - **Frontend Service (stateless gateway)** — API surface; routes requests to History and Matching.
  - **History Service (sharded, stateful)** — advances and persists workflow state. The **History Shard count is the master, immutable scaling parameter** (≈1 History instance per ~500 shards). Exchanges work bidirectionally with Matching (`HIST <--> MATCH`).
  - **Matching Service (task queues)** — dispatches workflow/activity tasks from task queues to polling workers.
  - **Internal Worker Service (system workflows)** — runs Temporal's own internal/system workflows.
- **Persistence: Cassandra / MySQL / PostgreSQL** — clustered durable store written by History, Matching, and the Internal Worker (Cassandra, MySQL 8+, or PostgreSQL 12+).
- **Visibility: Elasticsearch / OpenSearch or SQL** — list/search index. Standard visibility lives in the same SQL store; advanced visibility uses Elasticsearch/OpenSearch. Written by History, read by the Frontend.
- **Multi-cluster replication → Standby cluster + DB** — the DR target; the Internal Worker async-replicates to a standby cluster (active→passive).

HA baseline is multiple replicas + a clustered DB with multi-cluster async replication. In practice the persistence DB and the ES cluster are the real bottleneck, not the service tier. **Temporal Cloud HA namespaces** add same-region / multi-region / multi-cloud (AWS↔GCP) active-standby with automatic failover (~20-min RTO, sub-1-min RPO, 99.99% SLA).
