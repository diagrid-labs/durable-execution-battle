---
layout: default
footer: |
  <span>Marc Duiker</span>
  <img src=".demo/assets/diagrid-logo.webp" alt="Diagrid" style="height: 20px; width: auto;">
---

# Learning curve — two cliffs

**Workflow-as-code (Cadence, Temporal, Dapr, Restate)** → the **determinism cliff**: replay forces no clocks, no randomness, no I/O outside activities.

**Workflow-as-data (Conductor)** → no determinism rules; you learn the **JSON DSL** + the task-type catalog instead.

Restate has the loosest determinism rules — most "normal Node code" tolerated. Temporal has the steepest tutorial path *and* the best one (`learn.temporal.io`).
