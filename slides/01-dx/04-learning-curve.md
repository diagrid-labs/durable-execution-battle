---
layout: default
---

# Learning curve — two cliffs

**Workflow-as-code (Cadence, Temporal, Dapr, Restate)** → the **determinism cliff**: replay forces no clocks, no randomness, no I/O outside activities.

**Workflow-as-data (Conductor)** → no determinism rules; you learn the **JSON DSL** + the task-type catalog instead.

Restate has the loosest determinism rules — most "normal Node code" tolerated. Temporal has the steepest tutorial path *and* the best one (`learn.temporal.io`).
