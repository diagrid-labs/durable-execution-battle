# Restate — USS Enterprise diagnostics (TypeScript)

One of five framework demos for the *Durable Execution Battle* talk. Restate is the surprise of the talk: a single binary plus ~200 lines of TypeScript runs the same scenario the other four frameworks need a multi-container stack to host.

## Stack

- Node 22 LTS · TypeScript 5.7
- `@restatedev/restate-sdk` ^1.6 (handler side)
- `@restatedev/restate-sdk-clients` ^1.6 (Express wrapper side)
- Express ^4.21 (thin HTTP wrapper for the on-stage `.http` file)
- `restate-server` binary (RocksDB embedded; no Postgres, no Kafka, no Cassandra)

## Prerequisites

- Node 22+ (`node --version`)
- `restate-server` 1.6.x — `brew install restatedev/tap/restate-server` (macOS) or Docker fallback `docker run -p 8080:8080 -p 9070:9070 restate/restate:1.6.x`
- Approximately 200 MB free RAM for `restate-server`

## One-time setup

```bash
cd demos/05-restate
npm install
```

`package-lock.json` is committed; `npm ci` is preferred on the talk laptop for reproducible installs.

## Run the demo

Three terminals:

```bash
# Terminal 1 — Restate server (single binary, no Docker required on macOS)
# Pin the advertised address to loopback so auto-provisioning works regardless of
# what VPN/VM/Docker interfaces are up (a multi-homed hostname makes the node hang
# on "Trying to join the cluster ... Has the cluster been provisioned, yet?").
RESTATE_ADVERTISED_ADDRESS="http://127.0.0.1:5122/" restate-server

# Terminal 2 — Node process: handler endpoint + Express wrapper + auto-registration
cd demos/05-restate
npm run dev

# Terminal 3 — start a workflow
curl -X POST http://localhost:3000/start \
  -H 'Content-Type: application/json' \
  -d @- << 'EOF'
{
  "id": "enterprise-ncc-1701",
  "shipName": "USS Enterprise NCC-1701-D",
  "diagnosticsDate": "2370-04-08",
  "engineerName": "Geordi La Forge"
}
EOF
```

Watch the Restate admin UI at `http://localhost:9070` → **Invocations** → `enterprise-ncc-1701`.

## Kill-and-resume choreography

1. Trigger `POST /start` — the 5 scan activities appear as `Call(activities/scan*)` journal entries and complete in ~1 s.
2. The workflow enters a 60-second durable timer (the kill point) — `Sleep(60s)` shows in the journal with a live countdown.
3. **At T+20 s** (during the timer): `kill -9 $(pgrep -f 'tsx src/index')` (or `Ctrl+C` Terminal 2 twice).
4. The Restate admin UI shows the invocation transitioning to `SUSPENDED` / `RETRY_BACKOFF`. The server still holds the journal — the durable state survives.
5. Restart `npm run dev` in Terminal 2. The handler re-registers automatically.
6. The Restate server re-invokes the workflow; the SDK replays the journal up to `Sleep(60s)` and resumes the wait.
7. When the timer fires, `prioritizeIssues` runs; if severity is `yellow_alert` / `red_alert` (overwhelmingly likely thanks to the rigged health range), `notifyBridge` fires.
8. A `🚨 BRIDGE ALERT` line at WARN level appears in Terminal 2's stdout.

## API

| Method | Path | Purpose |
|---|---|---|
| `POST` | `http://localhost:3000/start` | Start a workflow with the given input. Returns 202 with `{ workflowId }`. |
| `GET` | `http://localhost:3000/status/:id` | If complete, returns `{ status: "COMPLETED", output }`. Otherwise returns 202 with `{ status: "RUNNING" }`. The real UI is the Restate admin panel at `:9070`. |

## Ports

| Port | Owner | Purpose |
|---|---|---|
| `3000` | Express wrapper | `/start`, `/status/:id` — what the audience POSTs to |
| `8080` | `restate-server` | Restate ingress (where the Express wrapper sends invocations) |
| `9070` | `restate-server` | Restate admin API + UI |
| `9080` | Node process | Restate SDK handler endpoint (Restate calls *back* into Node here) |

## Tests

```bash
npm test
```

Runs unit tests for `generateScan` invariants (rigged health range, anomaly count, no duplicates), `prioritize` threshold table (`<50` / `<75` / `≥75`), and the `formatBridgeAlert` message template. No Restate server required — pure logic tests via Node's built-in `node:test` runner.

## Comparison to the canonical Dapr (.NET) reference

The companion demo at [`../04-dapr/`](../04-dapr/) is the canonical reference. The structural inversion:

- Workflow body: `await Promise.all(client.scan*(...))` here vs `await Task.WhenAll(context.CallActivityAsync<T>(nameof(...))` there — same algorithm, two host languages.
- Activities: one `restate.service` with 7 inline handlers here vs 7 separate `WorkflowActivity<T,U>` subclass files there.
- Operational footprint: `restate-server` binary + 1 Node process here vs Aspire AppHost + Dapr sidecar + Valkey + Diagrid Dashboard there.
- Worker model: Restate **pushes** invocations to the handler over HTTP. Dapr (and Temporal, Cadence, Conductor) all use long-polling workers. That's the single biggest model-level difference among the five frameworks.

## Talking points on stage

- *"There's no worker. Restate is calling my HTTP handler. When I `kill -9` the Node process, the journal lives in the server — Restate retries until the handler is back, then replays from the journal."*
- *"One binary. No Postgres, no Kafka, no Cassandra. The whole demo is a `restate-server` binary plus `npm run dev`."*
- *"`Promise.all` over `client.scan*`. That's not magic — that's TypeScript. Each call is one journal entry."*
- *"`ctx.sleep` is the durable timer. When I kill Node mid-sleep, the journal entry says 'sleep until T+60s'. When Node is back, the SDK replays to that entry and waits for it to resolve."*

## Pin / verify before the talk

- `restate-server --version` matches the pinned line in this README.
- `npm ci` from a clean cache completes in <30 s on the talk laptop's warm Node cache.
- `package-lock.json` is committed and reproducible.
- SDK method names (`workflowClient` / `workflowSubmit` / `workflowOutput` with `Output<T>.ready`/`.result`) match `node_modules/@restatedev/restate-sdk-clients/dist/*.d.ts` for the pinned version. If not, `src/api.ts` is the one place to update.
- Full end-to-end dry run including the kill-and-resume sequence on the talk laptop, twice in a row.
- `npm test` passes from a clean checkout.
- Ports `3000`, `8080`, `9070`, `9080` are all free (`lsof -i :3000 -i :8080 -i :9070 -i :9080`).
