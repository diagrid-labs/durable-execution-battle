# Performance — local burst (testing=true, wait skipped)

| Engine    | N   | Wallclock | Throughput | p50    | p95    | p99    | Errors | Run at           |
|-----------|-----|-----------|------------|--------|--------|--------|--------|------------------|
| cadence   | 100 | 5.3 s     | 18.7 wf/s  | 3928 ms | 5270 ms | 5328 ms | 0      | 2026-05-29 12:27 |
| temporal  | 100 | 1.8 s     | 56.6 wf/s  | 1559 ms | 1731 ms | 1764 ms | 0      | 2026-05-29 12:30 |
| conductor | 100 | 61.5 s    | 1.0 wf/s   | 43562 ms | 61453 ms | 61455 ms | 39     | 2026-05-29 13:24 |
| dapr      | 100 | 2.4 s     | 41.2 wf/s  | 1139 ms | 1433 ms | 2422 ms | 0      | 2026-05-29 13:28 |
| restate   | 100 | 60.5 s    | 1.7 wf/s   | 607 ms | 1270 ms | 60479 ms | 0      | 2026-05-29 13:56 |

_Methodology: single-node local engines, default config; bursts of N workflows with `testing=true` (60s wait skipped); 200 ms status-poll floor; 5-workflow warmup discarded._
