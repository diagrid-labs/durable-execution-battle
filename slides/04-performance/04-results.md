# Performance — local burst (testing=true, wait skipped)

| Engine    | N   | Wallclock | Throughput | p50    | p95    | p99    | Errors | Run at           |
|-----------|-----|-----------|------------|--------|--------|--------|--------|------------------|
| cadence   | —   | _pending_ |            |        |        |        |        |                  |
| temporal  | —   | _pending_ |            |        |        |        |        |                  |
| conductor | —   | _pending_ |            |        |        |        |        |                  |
| dapr      | —   | _pending_ |            |        |        |        |        |                  |
| restate   | —   | _pending_ |            |        |        |        |        |                  |

_Methodology: single-node local engines, default config; bursts of N workflows with `testing=true` (60s wait skipped); 200 ms status-poll floor; 5-workflow warmup discarded._
