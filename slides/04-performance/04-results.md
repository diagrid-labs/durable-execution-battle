# Performance — local burst (testing=true, wait skipped)

| Engine    | N   | Wallclock | Throughput | p50    | p95    | p99    | Errors | Run at           |
|-----------|-----|-----------|------------|--------|--------|--------|--------|------------------|
| cadence   | 100 | 6.0 s     | 16.7 wf/s  | 4478 ms | 5835 ms | 5974 ms | 0      | 2026-05-29 19:59 |
| temporal  | 100 | 1.7 s     | 58.3 wf/s  | 1503 ms | 1702 ms | 1708 ms | 0      | 2026-05-29 20:02 |
| conductor | 100 | 91.3 s    | 0.7 wf/s   | 41265 ms | 91307 ms | 91308 ms | 38     | 2026-05-29 20:06 |
| dapr      | 100 | 2.2 s     | 46.4 wf/s  | 1307 ms | 1423 ms | 2149 ms | 0      | 2026-05-29 20:08 |
| restate   | 100 | 60.3 s    | 1.7 wf/s   | 530 ms | 1145 ms | 60311 ms | 0      | 2026-05-29 20:11 |

_Methodology: single-node local engines, default config; bursts of N workflows with `testing=true` (60s wait skipped); 200 ms status-poll floor; 5-workflow warmup discarded._
