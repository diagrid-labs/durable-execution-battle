---
layout: default
---

# Operational footprint

**Temporal cluster** — 4 sharded services (frontend, history, matching, worker) + Cassandra + Elasticsearch.

**Restate** — *one binary*. RocksDB on disk. That's it.

```
Temporal:   [ frontend ] [ history ] [ matching ] [ worker ]
                              │
                       [ Cassandra ]  [ Elasticsearch ]

Restate:    [ restate-server ──► RocksDB ]
```

For a hobby project or a side service: Restate. For a production cluster with audit + search: Temporal.
