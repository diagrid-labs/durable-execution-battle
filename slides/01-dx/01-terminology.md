---
layout: default
footer: |
  <span>Marc Duiker</span>
  <img src=".demo/assets/diagrid-logo.webp" alt="Diagrid" style="height: 20px; width: auto;">
---

# Same idea, different word

| | Cadence | Temporal | Conductor | Dapr | Restate |
|---|---|---|---|---|---|
| *Orchestration unit* | Workflow | Workflow | Workflow | Workflow | Workflow / Service |
| *Side-effect unit* | Activity | Activity | Task | Activity | Handler |
| *User-code process* | Worker | Worker | Worker | App | Service |
| *Routing* | Task List | Task Queue | Task Queue | Actor placement | Partition (key-hashed) |
| *Durable log* | Event history | Event history | Execution log | Execution history | Journal |
| *Stateful object* | — | — | — | Actor | Virtual Object |
