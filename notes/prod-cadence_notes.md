# Notes for Production — Cadence

The diagram shows a horizontally-scaled cluster of four Go services in front of a durable store, plus a standby cluster in another region.

Nodes in the diagram:

- **Workers and Clients** — your worker processes and CLI/SDK clients; they all enter through the load balancer and talk only to the Frontend.
- **Load Balancer** — spreads incoming traffic across the stateless Frontend instances.
- **Frontend service (xN)** — stateless gateway / API surface. Horizontally scalable; fans requests out to History, Matching, and Elasticsearch.
- **History service (xN — sharded by `numHistoryShards`)** — the stateful core that advances and persists workflow execution state. Split into a **fixed, immutable** number of shards (`numHistoryShards`) distributed over the Ringpop hash ring; each shard serializes its workflows, so the shard count is the unit of parallelism and must be over-provisioned up front.
- **Matching service (xN — partitioned task lists)** — the task-dispatch layer. Holds pending workflow/activity tasks on partitioned task lists and hands them to whichever worker is long-polling. History pushes generated work into Matching (`hist --> match`).
- **Internal Worker service** — runs Cadence's own system workflows; drives History.
- **Ringpop hash ring / membership** — gossip-based membership and consistent-hash ring that the Frontend, History, and Matching services use to discover each other and to assign History shards.
- **Persistence: Cassandra / SQL** — the durable store written by History and Matching.
- **Kafka → Elasticsearch / OpenSearch** — the Internal Worker streams visibility data through Kafka into the search/index layer; the Frontend reads it back for list/search queries.
- **Standby cluster (other region)** — a mirror Frontend → History → Persistence stack for multi-region HA.

Multi-region HA uses **Cross-DC Replication (XDC)** with Global Domains in **active-passive only** — the History service async-replicates to the standby region's History (`hist -. XDC .-> histB`). A domain is ACTIVE in exactly one cluster, with operator-triggered failover and at-least-once activity semantics across the switch.
