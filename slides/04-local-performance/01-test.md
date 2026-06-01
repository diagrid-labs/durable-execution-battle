---
layout: default
---

# The local performance test

- Runs **100 instances** of the `DiagnosticsWorkflow` against **one engine at a time**, running locally.
- `testing=true` skips the 60s wait, so we measure **orchestration overhead**, not application logic.
- Each engine runs as a **single-node local stack** on default config: a burst of `/start` calls, then poll status until every workflow completes.
- Records **wall-clock time, throughput, and per-workflow duration** — the 200 ms status-poll interval is the latency floor.

*This does not provide any information on production performance!*