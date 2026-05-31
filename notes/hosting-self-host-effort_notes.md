# Notes for Self Hosting — the effort matrix

These notes explain the **Self-host effort** column in `slides/03-hosting-production/01-hosting-matrix.md`. Ranking is grounded in `docs/research.md` (§"Self-host complexity", lines 712–718) and the Local Dev / Production Architecture sections of `docs/research-extended-areas.md`.

## What the column measures

Operational burden to stand up and run the engine **yourself**: number of long-running processes, mandatory external dependencies (database, search index, broker), and how much of that you must operate. It is **not** about throughput or feature richness.

## High — Cadence & Temporal

Heaviest footprint. The engine is a cluster of **four separate Go services** (frontend, history, matching, internal worker) in front of a mandatory external datastore, plus a search tier for advanced visibility.

- Storage: Cassandra / MySQL / Postgres **+ Elasticsearch/OpenSearch**.
- Most-cited complaint: "Cassandra + Elasticsearch is the most-cited self-host complaint" (`research.md:125`); Temporal carries the "heaviest self-host footprint... steepest determinism learning curve" (`research.md:174`).
- **Immutable-shard gotcha**: `numHistoryShards` (Cadence) / History Shard count (Temporal) is fixed at cluster creation — over-provision up front.
- K8s: Temporal ships an **official Helm chart**; Cadence only a **community chart** (`banzaicloud`).
- Functionally twins on effort (shared lineage) → both High.

## Medium — Conductor

A notch lighter: the **server itself is stateless** (scale by adding nodes), but it still needs a **multi-component external data tier**.

- Redis (state/queues) **+** relational/NoSQL store (Postgres/MySQL/Cassandra) **+** Elasticsearch/OpenSearch.
- Search index is technically optional but practically required — "the UI is mostly broken without it" (`research-extended-areas.md:94`).
- Local dev alone wants **~16 GB RAM** (JVM server + Elasticsearch).
- Fewer engine moving parts than Temporal/Cadence, but a real multi-service data tier → Medium.

## Low — Dapr

**No dedicated workflow cluster.** The engine (`durabletask-go`) is embedded in the `daprd` **sidecar**, so workflows scale with your existing app pods.

- Provide **one state store** — any transactional + ETag store you may already run (Postgres, Redis, SQL Server, Cosmos, CockroachDB...). **No mandatory search tier.**
- A small control plane (Placement, Scheduler, Sentry, Operator, Injector) keeps it "Low" rather than "Lowest" — but if you already run Dapr, workflows add **zero new long-running services**.

## Lowest — Restate

A **genuinely single binary** with **no external database, broker, or search index**.

- One `restate-server` (Rust) bundles runtime, embedded RocksDB, the Bifrost replicated log, the Raft metadata store, ingress, admin API, and the Web UI.
- The same artifact runs on a laptop and as a production cluster; HA just means more nodes of the same binary (3 nodes / RF 2).
- K8s: mature **operator (v2.5.1)**, `RestateCluster` CRD stable.

## Caveats to flag verbally

- The column conflates **single-node simplicity** with **HA effort**. Restate's HA (replicated log, quorum, location-aware placement) and Dapr's HA (delegated to datastore + Scheduler etcd) still require distributed-systems thought; the Low/Lowest rating reflects dependency/binary count, not zero work.
- **Diagrid bias**: Dapr and Restate are direct competitors and the author works at Diagrid (Dapr's company). Honest framing of the Low-vs-Lowest gap: Restate has fewer dependency types to operate (one binary, embedded storage); Dapr reuses infrastructure you likely already run if you're a Dapr shop. Both are legitimately far below the Temporal/Cadence/Conductor tier.
