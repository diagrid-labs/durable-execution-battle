---
layout: default
footer: |
  <span>Marc Duiker</span>
  <img src=".demo/assets/diagrid-logo.webp" alt="Diagrid" style="height: 20px; width: auto;">
---

# Production — Conductor

```mermaid
graph TD
  Clients[SDK clients / apps] --> LB[Load Balancer]
  WorkerFleet[Worker fleets - K8s] -->|poll task queues| LB
  UI[Web UI] --> LB
  subgraph ServerCluster[Stateless server fleet - multi-AZ]
    S1[Conductor Server node 1]
    S2[Conductor Server node 2]
    S3[Conductor Server node 3]
  end
  LB --> S1
  LB --> S2
  LB --> S3
  S1 --> Redis[(Redis / Redis Cluster - queues + state)]
  S2 --> Redis
  S3 --> Redis
  S1 --> RDB[(Postgres / MySQL / Cassandra - durable store)]
  S2 --> RDB
  S3 --> RDB
  S1 --> ES[(Elasticsearch / OpenSearch - index)]
  S2 --> ES
  S3 --> ES
  Brokers[Kafka / NATS / SQS / AMQP] -. Event tasks .- S1
```

A horizontally scalable cluster of **stateless** Conductor Server nodes (run ≥3 across AZs) behind a load balancer, sharing an external HA data tier: Redis (or Redis Cluster) for state/queues, a relational/NoSQL store for durability, and Elasticsearch/OpenSearch for indexing. Worker fleets are independent and poll the servers; brokers integrate via Event tasks.

Because the engine layer is stateless, you scale it just by adding nodes — pressure falls on the **data tier** (Redis and Postgres CPU/IOPS), which must run replicated for HA. Orkes advertises up to 99.99% with auto-failover; active-active multi-region is an enterprise/contract concern.
