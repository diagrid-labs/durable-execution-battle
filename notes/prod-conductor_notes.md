# Notes for Production — Conductor

The diagram shows a fleet of **stateless** Conductor Server nodes behind a load balancer, all sharing one external HA data tier (Redis + relational/NoSQL store + search index).

Nodes in the diagram:

- **SDK clients / apps** — start and query workflows through the load balancer.
- **Worker fleets (K8s)** — independent worker processes that **poll the task queues** through the load balancer; scale separately from the server.
- **Web UI** — the Conductor UI, also routed via the load balancer.
- **Load Balancer** — fans all client/worker/UI traffic across the server nodes.
- **Stateless server fleet (multi-AZ)** — the subgraph of **Conductor Server node 1/2/3** (run ≥3 across availability zones). Because the engine layer is stateless, you scale it just by adding nodes; every node talks to the same shared data tier.
- **Redis / Redis Cluster (queues + state)** — holds task queues and hot workflow state.
- **Postgres / MySQL / Cassandra (durable store)** — the durable system-of-record for workflow execution.
- **Elasticsearch / OpenSearch (index)** — search/index layer for list and query operations.
- **Kafka / NATS / SQS / AMQP (Event tasks)** — external brokers that integrate with the server via Event tasks (the dashed link to a server node).

Because the engine is stateless, scaling the server tier is trivial — pressure instead falls on the **data tier** (Redis and Postgres CPU/IOPS), which must run replicated for HA. Orkes (the commercial offering) advertises up to 99.99% with auto-failover; active-active multi-region is an enterprise/contract concern.
