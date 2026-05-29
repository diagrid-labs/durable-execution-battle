# Performance driver

Runs N copies of the shared `DiagnosticsWorkflow` against **one** workflow
engine at a time and writes the results into
`slides/04-performance/04-results.md`. Each demo's `DiagnosticsInput` has a
`testing` flag that, when `true`, skips the 60s engineering-review wait so the
workflow body runs as fast as the engine allows.

## Run one engine

Start the engine's stack first (see the engine's own README under
`demos/0X-<engine>/`). Then:

```bash
cd demos/perf
npm install            # first time only
npm run bench -- --engine cadence --n 100
```

Replace `cadence` with `temporal | conductor | dapr | restate`. One engine
per invocation — the driver does not coordinate multiple stacks.

## Flags

| Flag | Default | Meaning |
|---|---|---|
| `--engine` | *(required)* | `cadence` / `temporal` / `conductor` / `dapr` / `restate` |
| `--n` | 100 | Number of measured workflows |
| `--concurrency` | `--n` | Max in-flight `/start` calls (defaults to full burst) |
| `--warmup` | 5 | Throwaway workflows run before the measured set; not recorded |
| `--reset` | off | Reset this engine's row in `04-results.md` to `_pending_` before the run |
| `--poll-ms` | 200 | Status-poll interval. Latency floor. |
| `--timeout-s` | 120 | Per-workflow timeout |
| `--base-url` | `http://localhost:8080` | Override the engine's base URL |

## Outputs

- Per-run CSV: `demos/perf/results/<engine>-<N>-<ISO>.csv` (one row per workflow)
- Aggregate table: `slides/04-performance/04-results.md` (this engine's row replaced; others untouched)

## Methodology caveats

These numbers are for a conference-talk comparison, not a production benchmark.

- All engines run as single-node local stacks on default config.
- 200 ms status-poll interval is a latency floor; p50 < ~250 ms is meaningless.
- Engines are benchmarked on separate driver invocations; the table records a
  timestamp per row.
- Conductor's wait skip uses a `SwitchTask` wrapping the `WaitTask`; the other
  four engines use an in-code conditional. The Conductor workflow therefore has
  one extra structural node in both branches.
- Activities are mock work — these numbers reflect orchestration overhead
  primarily, not application logic.
