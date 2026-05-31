---
layout: default
footer: |
  <span>Marc Duiker</span>
  <img src=".demo/assets/diagrid-logo.webp" alt="Diagrid" style="height: 20px; width: auto;">
---

# Why Dapr Workflow should be your default

✅ **Lowest operational footprint** — the engine runs inside the `daprd` sidecar. No separate workflow cluster, no Cassandra + Elasticsearch to operate. Already running Dapr? Workflows add **zero** new services.

✅ **Bring your own datastore** — any transactional + ETag store (Postgres, Redis, SQL Server, Cosmos…). No mandatory search-index tier.

✅ **One of 12 building blocks** — state, pub/sub, secrets, bindings, service invocation with mTLS — same sidecar, callable from workflow code.

✅ **The only CNCF-graduated option** — vendor-neutral governance, Apache 2.0, no open-core gating, no single-vendor lock-in.

✅ **Polyglot workflow management over plain HTTP** — start, signal, and query a workflow from `curl`. No SDK required.

✅ **Versioning is first-class** — 1.17 added patching + named versions; 1.18 adds tamper detection, concurrency caps, and access policies.
