# Restate (TypeScript) demo — design

**Status:** approved design, ready for implementation plan
**Author:** Marc Duiker
**Date:** 2026-05-17
**Target:** `demos/05-restate/`
**Canonical scenario:** [`demos/shared/scenario.md`](../../../demos/shared/scenario.md) (mirrored verbatim from `plan.md` §4)
**Canonical reference implementation:** [`demos/04-dapr/`](../../../demos/04-dapr/) (.NET 10 + Aspire — see `plan.md` §5.4)
**Position in talk:** final demo of the 25-min block (`plan.md` §1, t=38:00, 5 min); the "surprise of the talk" slot the closing-poll narration hinges on (§3.5 Poll B reactions).

---

## 1. Goal

Implement the USS Enterprise diagnostics workflow in **Restate with TypeScript** as the final framework demo for the "Durable Execution Battle" talk. The implementation must:

- Match the **canonical shared scenario** (§4 of `plan.md`) on activity behavior, severity thresholds, rigging strategy, and the kill-and-resume choreography.
- Hold the **cross-demo wire invariants** from `plan.md` §5.0 constant — subsystem strings (`warp_core`, `shields`, `life_support`, `sensors`, `weapons`), severity wire values (`green`, `yellow_alert`, `red_alert`), health range `30..65`, bridge alert message format, workflow id `enterprise-ncc-1701`.
- Be **idiomatic Restate + TypeScript** — the workflow is `restate.workflow({...})`; activities are a sibling `restate.service({...})` with 7 handlers; fan-out uses `Promise.all` over typed service-client calls; sleep is `ctx.sleep`. **No polling**, no worker pool — Restate pushes invocations to the handler endpoint over HTTP.
- Run from a fresh laptop in **under 90 seconds** of cold start: `restate-server` binary in one terminal, `npm run dev` in another. **Single binary, no Postgres / no Kafka / no Cassandra** — the operational footprint *is* the talking point (`plan.md` §5.5).
- **Be the surprise demo.** Audience just sat through five-container-per-stack frameworks; here a single binary plus 200 lines of TypeScript does the same job with the same durability guarantees.

## 2. Locked decisions

| Choice | Decision | Why |
|---|---|---|
| **Language / runtime** | TypeScript 5.x on Node 22 LTS | Restate's most mature SDK and most idiomatic surface |
| **SDK** | `@restatedev/restate-sdk` (handler side) + `@restatedev/restate-sdk-clients` (Express wrapper side) | Both are first-party. Clients SDK provides typed `workflowSendClient` so the Express wrapper stays type-safe end-to-end. |
| **Activity hosting** | **Sibling Service with 7 handlers** in `src/services.ts` — one `restate.service({ name: "activities", handlers: { scanWarpCore, ... } })`; workflow calls via `ctx.serviceClient(activities)` | Matches `plan.md`'s named layout (`src/services.ts`); each scan appears as its own journal entry with the handler's name — far more readable in the Restate UI than one blurred `ctx.run`. Sibling-service form also makes the §5.5 "Restate is *calling* my handler" point literal — there is a named, addressable service. |
| **Notify bridge** | Sibling-service handler `notifyBridge` on the same `activities` service (not inline) | Consistency: every side-effect is a named, addressable handler. The bridge alert lands as `activities/notifyBridge` in the journal, which is the easier on-stage point ("this entry is the bridge alert; it survived the kill"). |
| **Workflow primitive** | `restate.workflow({ name: "DiagnosticsWorkflow", handlers: { run: ... } })` — single `run` handler | Restate's Workflow primitive is single-shot run-to-completion with a strong workflow id; matches the scenario's one-shot semantics. Virtual Object is wrong here (no long-lived state across runs). |
| **HTTP API surface** | Thin Express wrapper exposing `POST /start` and `GET /status/{id}` on port `3000` | Mirrors the Temporal/Cadence/Conductor/Dapr demos' `.http` file shape so audience-side comparison is clean. Uses `@restatedev/restate-sdk-clients` `workflowSendClient` to kick off and `workflowClient(...).workflowAttach()` (or REST query) to observe. |
| **Process model** | **Single Node process** — `src/index.ts` binds the workflow + service via `restate.endpoint().bind(diagnosticsWorkflow).bind(activities).listen(9080)` *and* starts the Express wrapper on `3000` in the same process | One `npm run dev` for the audience; one `kill -9` kills both the handler endpoint and the Express wrapper, dramatising the durability beat (Restate survives; the workflow resumes when Node is back). |
| **Restate server** | Native binary (`restate-server`) installed via `brew install restatedev/tap/restate-server` (or curl), pinned in README to `1.6.x` | `plan.md` §5.5 hinges on the "one binary, no Postgres, no Kafka, no Cassandra" line — running Restate *itself* as a binary instead of Docker makes the line literal. README also documents `restate/restate:1.6.2` as the Docker fallback for audience members. |
| **Restate ports** | Ingress `8080`, admin `9070` (UI), node handler endpoint `9080` | Restate defaults — change nothing. The Express wrapper takes `3000` to stay out of the way. |
| **Wire format** | camelCase on the wire (TypeScript's natural shape); subsystem and severity *string values* stay snake_case (`warp_core`, `red_alert`) since those are cross-demo invariants | TypeScript convention is camelCase; the wire-format invariants from `plan.md` §5.0 are about *string values*, not field names. Audience reading the `.http` body will see `shipName` (vs Conductor/Dapr's `ship_name`) — that's the idiomatic delta the talk explicitly wants visible. |
| **Auto-deployment registration** | On Express startup, POST `http://localhost:9070/deployments` with `{ "uri": "http://localhost:9080" }`; tolerate 409 (already registered) | Removes the manual `restate deployments register` step from the on-stage runbook. Idempotent on restart. |
| **Build tooling** | `tsx` for dev (no compile step on stage); `tsc` for type-checking | `npm run dev` → instant restart on file save; no `dist/` clutter. Type-checking via `npm run check` runs in CI / pre-talk only. |
| **Dependency footprint** | `@restatedev/restate-sdk`, `@restatedev/restate-sdk-clients`, `express`, `tsx`, `typescript`, `@types/express`, `@types/node` | 7 deps total. The Cadence/Conductor demos pull dozens of Java/.NET transitives; the Restate `package.json` is *the* visual proof of the small-footprint pitch. |

## 3. Project layout

```
demos/05-restate/
├── README.md                          # run instructions + talk choreography
├── package.json
├── tsconfig.json
├── enterprise-diagnostics.http        # POST /start, GET /status/{id} — VS Code REST Client
│
└── src/
    ├── index.ts                       # entry point: binds Restate endpoint + starts Express + auto-registers deployment
    ├── api.ts                         # Express wrapper: POST /start, GET /status/{id}
    ├── diagnostics.ts                 # restate.workflow — the run handler
    ├── services.ts                    # restate.service "activities" with 7 handlers
    ├── mock.ts                        # MockAnalysisGenerator equivalent (health 30..65 + per-subsystem anomaly samples)
    └── models.ts                      # shared TS types/interfaces
```

**Structural delta vs the Dapr demo** (the canonical reference):

```
┌───────────────────────── deltas vs Dapr ──────────────────────────┐
│ + src/index.ts                  (single binding point; no Aspire) │
│ + src/api.ts                    (Express thin wrapper)            │
│ ~ src/diagnostics.ts            (was Workflows/DiagnosticsWorkflow │
│     was: override RunAsync(WorkflowContext, ...)                   │
│     now: restate.workflow({...handlers: {run: async(ctx,input)}}) │
│ ~ src/services.ts               (was Activities/Scan*Activity.cs) │
│     was: 7 WorkflowActivity<T,U> subclasses, one per file         │
│     now: 1 restate.service with 7 handler functions               │
│ - EnterpriseDiagnostics.AppHost                                    │
│ - EnterpriseDiagnostics.ServiceDefaults                            │
│ - Valkey container                                                 │
│ - Dapr sidecar                                                     │
│ - Diagrid Dashboard container                                      │
│ + restate-server binary         (everything Dapr-side replaced by  │
│                                  one process)                      │
└────────────────────────────────────────────────────────────────────┘
```

The visible-on-stage fact: the entire Dapr `Activities/` folder (5 files + `MockAnalysisGenerator` + `PrioritizeIssuesActivity` + `NotifyBridgeActivity`) collapses to **one TypeScript file** (`services.ts`) with 7 inline handler functions — and the Aspire AppHost + sidecar + state store + dashboard collapse to `restate-server`.

## 4. Model shapes (TypeScript types)

`src/models.ts` — colocated types matching the canonical Dapr records on *value shape* but using camelCase field names per TS convention.

```typescript
// src/models.ts

export type Severity = "green" | "yellow_alert" | "red_alert";

export const Subsystem = {
  WarpCore:    "warp_core",
  Shields:     "shields",
  LifeSupport: "life_support",
  Sensors:     "sensors",
  Weapons:     "weapons",
} as const;
export type SubsystemId = typeof Subsystem[keyof typeof Subsystem];

export interface DiagnosticsInput {
  id: string;                   // workflow id, e.g. "enterprise-ncc-1701"
  shipName: string;
  diagnosticsDate: string;
  engineerName: string;
}

export interface DiagnosticsOutput {
  shipName: string;
  diagnosticsDate: string;
  engineerName: string;
  priority: Priority;
  bridgeNotification: string | null;   // null = green path
}

export interface ScanInput {
  shipName: string;
  subsystem: SubsystemId;
}

export interface ScanResult {
  subsystem: SubsystemId;
  health: number;             // 30..65 inclusive (rigged)
  anomalies: string[];        // 1-3 entries, sampled per subsystem
}

export interface PrioritizeInput {
  shipName: string;
  scans: ScanResult[];
}

export interface Priority {
  severity: Severity;
  summary: string;
}

export interface BridgeNotificationInput {
  shipName: string;
  engineerName: string;
  priority: Priority;
}

export interface BridgeNotificationResult {
  message: string;
}
```

Severity is a **string literal union** (not an enum) so the wire form matches the cross-demo `green` / `yellow_alert` / `red_alert` invariant verbatim; no enum-to-string mapping needed.

## 5. Workflow definition

`src/diagnostics.ts` — the file the audience sees first.

```typescript
// src/diagnostics.ts
import * as restate from "@restatedev/restate-sdk";
import { activities } from "./services";
import type {
  DiagnosticsInput,
  DiagnosticsOutput,
  ScanResult,
  Priority,
} from "./models";

export const diagnosticsWorkflow = restate.workflow({
  name: "DiagnosticsWorkflow",
  handlers: {
    run: async (
      ctx: restate.WorkflowContext,
      input: DiagnosticsInput,
    ): Promise<DiagnosticsOutput> => {
      const client = ctx.serviceClient(activities);

      // ── Fan-out: 5 parallel subsystem scans ──
      const scans: ScanResult[] = await Promise.all([
        client.scanWarpCore({    shipName: input.shipName, subsystem: "warp_core"    }),
        client.scanShields({     shipName: input.shipName, subsystem: "shields"     }),
        client.scanLifeSupport({ shipName: input.shipName, subsystem: "life_support" }),
        client.scanSensors({     shipName: input.shipName, subsystem: "sensors"     }),
        client.scanWeapons({     shipName: input.shipName, subsystem: "weapons"     }),
      ]);

      // ── 60s durable sleep — ⚡ KILL POINT ⚡ ──
      ctx.console.log("Engineering review pending — sleeping 60s");
      await ctx.sleep({ seconds: 60 });

      // ── Prioritization ──
      const priority: Priority = await client.prioritizeIssues({
        shipName: input.shipName,
        scans,
      });

      // ── Conditional bridge notification ──
      let bridgeNotification: string | null = null;
      if (priority.severity === "red_alert" || priority.severity === "yellow_alert") {
        const result = await client.notifyBridge({
          shipName: input.shipName,
          engineerName: input.engineerName,
          priority,
        });
        bridgeNotification = result.message;
      }

      return {
        shipName: input.shipName,
        diagnosticsDate: input.diagnosticsDate,
        engineerName: input.engineerName,
        priority,
        bridgeNotification,
      };
    },
  },
});
```

**Determinism note for the audience.** This `run` handler is replayed every time Restate re-invokes the workflow (e.g. after a `kill -9` of the Node process during the 60s sleep). All side-effects — service calls, sleep, console.log via `ctx.console` — are journaled by the SDK. Plain `Math.random()` / `Date.now()` / `console.log()` would break replay. The `services.ts` handlers below are *where* non-determinism is allowed because each service call is itself one journal entry.

**On-stage talking points anchored to this file:**

- *"There is no worker. Restate is calling this handler over HTTP. When I `kill -9` the Node process, Restate retries. When Node comes back, Restate re-invokes from the journal."*
- *"`Promise.all` over `client.scan*`. That's not magic — that's just TypeScript. The fan-out is journaled because each service call is one entry."*
- *"`ctx.sleep` is the durable timer. The journal entry says 'sleep until T+60s'. When the handler comes back, it replays up to the sleep and waits for the entry to resolve."*

## 6. Activity service

`src/services.ts` — one `restate.service` with 7 handlers. Each handler is a typed async function; Restate journals each call as its own entry visible in the admin UI.

```typescript
// src/services.ts
import * as restate from "@restatedev/restate-sdk";
import { generateScan, prioritize } from "./mock";
import type {
  ScanInput,
  ScanResult,
  PrioritizeInput,
  Priority,
  BridgeNotificationInput,
  BridgeNotificationResult,
} from "./models";

export const activities = restate.service({
  name: "activities",
  handlers: {
    // ── 5 scan handlers ──
    scanWarpCore:    async (_ctx: restate.Context, input: ScanInput): Promise<ScanResult> =>
      generateScan("warp_core",    warpCoreCandidates),
    scanShields:     async (_ctx: restate.Context, input: ScanInput): Promise<ScanResult> =>
      generateScan("shields",      shieldsCandidates),
    scanLifeSupport: async (_ctx: restate.Context, input: ScanInput): Promise<ScanResult> =>
      generateScan("life_support", lifeSupportCandidates),
    scanSensors:     async (_ctx: restate.Context, input: ScanInput): Promise<ScanResult> =>
      generateScan("sensors",      sensorsCandidates),
    scanWeapons:     async (_ctx: restate.Context, input: ScanInput): Promise<ScanResult> =>
      generateScan("weapons",      weaponsCandidates),

    // ── Prioritization (deterministic on real inputs) ──
    prioritizeIssues: async (_ctx: restate.Context, input: PrioritizeInput): Promise<Priority> =>
      prioritize(input.scans),

    // ── Bridge notification ──
    notifyBridge: async (
      _ctx: restate.Context,
      input: BridgeNotificationInput,
    ): Promise<BridgeNotificationResult> => {
      const message =
        `🚨 BRIDGE ALERT — USS ${input.shipName} — ` +
        `${input.priority.severity.toUpperCase()}: ${input.priority.summary} ` +
        `(engineer: ${input.engineerName})`;
      console.warn(message);
      return { message };
    },
  },
});

// Per-subsystem candidate anomaly lists — sampled by mock.ts
const warpCoreCandidates = [
  "plasma conduit micro-fracture (deck 36)",
  "warp coil phase variance 0.04%",
  "dilithium articulation frame torque drift",
  "matter/antimatter injector hesitation",
  "magnetic constriction segment 7 thermal creep",
];
const shieldsCandidates = [
  "shield grid harmonic decay (forward emitters)",
  "deflector dish calibration drift",
  "ablative armor micro-spalling",
  "structural integrity field fluctuation",
];
const lifeSupportCandidates = [
  "atmospheric scrubber CO2 saturation 0.6%",
  "deck 8 humidity sensor offline",
  "gravity plating phase variance",
  "ventilation manifold particulate buildup",
];
const sensorsCandidates = [
  "lateral sensor array calibration drift",
  "long-range subspace antenna polarization shift",
  "navigational deflector noise floor elevated",
  "tactical scanner refresh rate degraded",
];
const weaponsCandidates = [
  "phaser bank 3 thermal coupling intermittent",
  "torpedo loader servo lag",
  "targeting array micro-latency",
  "EPS conduit to weapons grid showing micro-arcs",
];
```

**No artificial latency.** Scans return as fast as Node can serialize the response — the 60s `ctx.sleep` in the workflow body is the only kill window. This matches the canonical Dapr scenario verbatim (`plan.md` §4: "No artificial scan latency").

**Why one service with 7 handlers (not 7 services).** Restate addresses services as `serviceName/handlerName`. Putting all 7 in one `activities` service means the journal entries read as `activities/scanWarpCore`, `activities/prioritizeIssues`, `activities/notifyBridge` — visually grouped under a single name in the UI. Seven separate services would fragment the admin UI's `Services` panel with one entry per scan. The deployment registration POSTs one URL (`http://localhost:9080`) regardless.

**Re: `_ctx` parameter.** Each handler accepts a `restate.Context` but doesn't use it (none of these need to call sub-services, set state, or sleep). The parameter is required by the SDK signature; the `_` prefix is the TS convention for intentionally-unused params.

### `src/mock.ts`

```typescript
// src/mock.ts
import type { ScanResult, Priority, Severity, SubsystemId } from "./models";

/**
 * Health is biased into [30, 65] so min(health) < 50 is overwhelmingly likely,
 * which makes severity reliably land on red_alert and the bridge alert fires
 * almost every run. Anomalies are 1-3 entries sampled from the per-subsystem list.
 *
 * NOTE: Math.random() lives inside the handler (a journaled service call),
 * not inside the workflow body. That's where non-determinism is allowed.
 */
export function generateScan(
  subsystem: SubsystemId,
  candidates: string[],
): ScanResult {
  const health = 30 + Math.floor(Math.random() * 36);   // 30..65 inclusive
  const count = 1 + Math.floor(Math.random() * 3);      // 1..3
  const shuffled = [...candidates].sort(() => Math.random() - 0.5);
  return {
    subsystem,
    health,
    anomalies: shuffled.slice(0, count),
  };
}

export function prioritize(scans: ScanResult[]): Priority {
  const worst = scans.reduce((min, s) => s.health < min.health ? s : min, scans[0]);
  const severity: Severity =
    worst.health < 50 ? "red_alert"    :
    worst.health < 75 ? "yellow_alert" :
                        "green";
  const belowThreshold = scans.filter(s => s.health < 75).length;
  const summary =
    `${worst.subsystem} critical at ${worst.health}% health; ` +
    `${belowThreshold} subsystem(s) below threshold`;
  return { severity, summary };
}
```

## 7. HTTP API (Express wrapper)

`src/api.ts` — a thin Express wrapper exposing the same `POST /start` / `GET /status/{id}` shape the other four demos use. Calls into Restate via `@restatedev/restate-sdk-clients` for type-safe workflow invocation.

```typescript
// src/api.ts
import express from "express";
import * as clients from "@restatedev/restate-sdk-clients";
import { diagnosticsWorkflow } from "./diagnostics";
import type { DiagnosticsInput } from "./models";

const RESTATE_INGRESS = process.env.RESTATE_INGRESS ?? "http://localhost:8080";
const PORT = Number(process.env.PORT ?? 3000);

export function buildApi() {
  const app = express();
  app.use(express.json());

  const restateClient = clients.connect({ url: RESTATE_INGRESS });

  // POST /start — kick off a diagnostics workflow.
  // Returns immediately with the workflow id; the actual run takes 60s+ (durable sleep).
  app.post("/start", async (req, res) => {
    const input = req.body as DiagnosticsInput;
    const handle = restateClient.workflowSendClient(diagnosticsWorkflow, input.id);
    await handle.run(input);
    res.status(202).json({ workflowId: input.id });
  });

  // GET /status/{id} — query the workflow's current status + (when complete) output.
  app.get("/status/:id", async (req, res) => {
    const id = req.params.id;
    const handle = restateClient.workflowClient(diagnosticsWorkflow, id);
    // workflowOutput() resolves when the workflow finishes; if still running, return 202.
    try {
      const output = await Promise.race([
        handle.workflowOutput(),
        new Promise((_, rej) => setTimeout(() => rej(new Error("pending")), 250)),
      ]);
      res.json({ status: "COMPLETED", output });
    } catch {
      res.status(202).json({ status: "RUNNING" });
    }
  });

  return app.listen(PORT, () =>
    console.log(`Express API listening on http://localhost:${PORT}`));
}
```

> **SDK-shape verification note.** The `@restatedev/restate-sdk-clients` method names (`workflowSendClient`, `workflowClient`, `workflowOutput`) target SDK v1.x. If the pinned version uses a different attach/output API (e.g. `attach()` instead of `workflowOutput()`), rename consistently. The Express surface (`POST /start`, `GET /status/{id}`) stays the same — only the underlying client call adjusts.

### `enterprise-diagnostics.http`

```http
### Start diagnostics
POST http://localhost:3000/start
Content-Type: application/json

{
  "id": "enterprise-ncc-1701",
  "shipName": "USS Enterprise NCC-1701-D",
  "diagnosticsDate": "2370-04-08",
  "engineerName": "Geordi La Forge"
}

### Status
GET http://localhost:3000/status/enterprise-ncc-1701
```

Note the body is **camelCase** (`shipName`, `diagnosticsDate`) — that's the idiomatic TypeScript wire form. The Conductor and Dapr demos' `.http` files use snake_case. That visible delta in the `.http` file is itself a §5.0 talking point ("each demo uses its language's native conventions").

## 8. Entry point

`src/index.ts` — one process binds the Restate handler endpoint, starts Express, and auto-registers the deployment with the Restate admin API.

```typescript
// src/index.ts
import * as restate from "@restatedev/restate-sdk";
import { diagnosticsWorkflow } from "./diagnostics";
import { activities } from "./services";
import { buildApi } from "./api";

const HANDLER_PORT = Number(process.env.HANDLER_PORT ?? 9080);
const ADMIN_URL = process.env.RESTATE_ADMIN ?? "http://localhost:9070";

// 1. Restate handler endpoint (HTTP/2 server bound by the SDK).
restate
  .endpoint()
  .bind(diagnosticsWorkflow)
  .bind(activities)
  .listen(HANDLER_PORT);

// 2. Express wrapper.
buildApi();

// 3. Auto-register deployment with Restate. Idempotent: 200/201 on first boot,
//    409 on subsequent boots (deployment already exists at this URI).
void (async () => {
  const uri = `http://localhost:${HANDLER_PORT}`;
  const res = await fetch(`${ADMIN_URL}/deployments`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ uri, force: true }),
  });
  if (res.ok || res.status === 409) {
    console.log(`Deployment registered at ${uri}`);
  } else {
    console.error(`Deployment registration failed: ${res.status} ${await res.text()}`);
    process.exit(1);
  }
})();
```

The `force: true` flag lets the registrar replace an existing deployment at the same URI without manual cleanup between demo runs (e.g. after a code edit + restart).

## 9. Runtime — how to run on stage

```bash
# Terminal 1 — Restate server (single binary)
restate-server

# Terminal 2 — Node process: handler endpoint + Express wrapper + auto-registration
cd demos/05-restate && npm run dev

# Terminal 3 — kick off
curl -X POST http://localhost:3000/start \
  -H 'Content-Type: application/json' \
  -d '{"id":"enterprise-ncc-1701","shipName":"USS Enterprise NCC-1701-D","diagnosticsDate":"2370-04-08","engineerName":"Geordi La Forge"}'
```

Audience watches the **Restate admin UI** at `http://localhost:9070` → Invocations → `enterprise-ncc-1701`. The visual progression:

1. Workflow appears as a `DiagnosticsWorkflow/run` invocation in state `RUNNING`.
2. The journal panel fills with 5 entries: `Call(activities/scanWarpCore)`, `Call(activities/scanShields)`, `Call(activities/scanLifeSupport)`, `Call(activities/scanSensors)`, `Call(activities/scanWeapons)` — all in flight, then completing near-instantly.
3. A new journal entry: `Sleep(60s)`. The UI shows the remaining duration.
4. *(Marc kills the Node process at ~T+20s into the sleep — `kill -9 $(pgrep -f 'tsx src/index')` or just Ctrl+C the terminal twice.)*
5. The admin UI shows the invocation is still tracked — Restate doesn't lose state when the handler dies, because the journal lives in the server.
6. *(Marc restarts: `npm run dev` in Terminal 2.)* The Node process boots, re-registers, and Restate re-invokes the handler. The SDK replays the journal up to `Sleep(60s)` and resumes waiting.
7. Sleep fires → `Call(activities/prioritizeIssues)` → `Call(activities/notifyBridge)` → invocation `COMPLETED`.
8. The `🚨 BRIDGE ALERT` line appears in the Node stdout. The admin UI's journal shows the `notifyBridge` call's output containing the same string.

**The Restate-specific kill-and-resume payoff (§5.5 verbatim):** *"There's no worker polling a task queue. Restate is calling my HTTP handler. The journal lives in the server, not the SDK. One binary, no Postgres, no Kafka, no Cassandra. When I `kill -9` the handler, Restate retries. When the handler is back, Restate re-invokes immediately — no poll interval to wait through. Watch the journal in the UI: entries light up green the moment the handler reconnects."*

The push-not-poll detail is the part to *land* on stage — it's the model-level difference vs Temporal/Cadence/Dapr/Conductor, all of which use long-polling workers.

## 10. `package.json`

```jsonc
{
  "name": "enterprise-diagnostics-restate",
  "version": "0.1.0",
  "private": true,
  "type": "module",
  "scripts": {
    "dev":   "tsx watch src/index.ts",
    "start": "tsx src/index.ts",
    "check": "tsc --noEmit"
  },
  "dependencies": {
    "@restatedev/restate-sdk":         "^1.6.0",
    "@restatedev/restate-sdk-clients": "^1.6.0",
    "express":                          "^4.21.0"
  },
  "devDependencies": {
    "@types/express": "^5.0.0",
    "@types/node":    "^22.0.0",
    "tsx":            "^4.19.0",
    "typescript":     "^5.7.0"
  },
  "engines": { "node": ">=22.0.0" }
}
```

> **Pin pre-talk.** Replace `^1.6.0` with exact versions a week before the talk and run `npm ci` from a clean cache to verify the lockfile reproduces. The `package-lock.json` should be committed.

## 11. `tsconfig.json`

```jsonc
{
  "compilerOptions": {
    "target": "ES2023",
    "module": "ESNext",
    "moduleResolution": "Bundler",
    "esModuleInterop": true,
    "strict": true,
    "skipLibCheck": true,
    "resolveJsonModule": true,
    "isolatedModules": true,
    "noUncheckedIndexedAccess": true,
    "outDir": "dist"
  },
  "include": ["src/**/*"]
}
```

`noUncheckedIndexedAccess` is on because the workflow code indexes arrays (`scans[0]` in `prioritize`); the option forces explicit null handling and matches the determinism-aware style used in `mock.ts`.

## 12. plan.md updates

No `plan.md` edits required — this spec is the implementation contract for the design already sketched in §5.5 and §5.0. Notable harmonizations this spec makes explicit (and which §5.5 left flexible):

- `plan.md` §5.5 says *"Activities are sibling services (or `ctx.run` blocks for inline)"* — this spec **chooses sibling services** for all 7 handlers (including `notifyBridge`, which §5.5 sketched as an inline `ctx.run`). Rationale in §2 + §6; matches the file-layout `src/services.ts` plan.md already names.
- `plan.md` §5.5 says *"Restate is calling my HTTP handler"* — this spec makes the audience-visible HTTP surface real via the Express `POST /start` wrapper, so the audience uses the same `.http` file shape across all five demos. The Restate ingress is still the runtime invocation path; the Express layer is sugar for stage consistency.

If after implementation the Express wrapper feels redundant on stage, removing it is a 1-line change to the README (audience POSTs to `http://localhost:8080/DiagnosticsWorkflow/{id}/run` directly). Spec stays valid either way.

## 13. Mapping — Dapr (.NET) vs Restate (TS) cheat sheet

The opposite end of the talk's framework spectrum from the Conductor-vs-Dapr pairing. Same scenario, opposite stack — useful for the decision-guide slide.

| Concern | Dapr (.NET, workflow-as-code) | Restate (TS, workflow-as-code) |
|---|---|---|
| **Workflow file** | `Workflow<TIn, TOut>` subclass, `RunAsync(context, input)` | `restate.workflow({ name, handlers: { run: async (ctx, input) => {...} } })` |
| **Fan-out** | `Task.WhenAll(activityTasks)` | `Promise.all(serviceClientCalls)` |
| **Sleep** | `await context.CreateTimer(TimeSpan.FromSeconds(60))` | `await ctx.sleep({ seconds: 60 })` |
| **Conditional branch** | `if (priority.Severity is "yellow_alert" or "red_alert") { await ... }` | `if (priority.severity === "yellow_alert" \|\| priority.severity === "red_alert") { await ... }` |
| **Activity / handler** | `WorkflowActivity<TIn, TOut>` subclass per side-effect | One `restate.service` with N handlers (or inline `ctx.run`) |
| **Activity invocation from workflow** | `await context.CallActivityAsync<T>(nameof(Foo), input)` | `await ctx.serviceClient(activities).foo(input)` (typed!) |
| **Activity registration** | `services.AddDaprWorkflow(o => o.RegisterActivity<Foo>())` | `restate.endpoint().bind(activities)` |
| **Workflow registration** | `services.AddDaprWorkflow(o => o.RegisterWorkflow<Diagnostics>())` (per-process) | `restate.endpoint().bind(diagnosticsWorkflow)` + admin API `POST /deployments` once |
| **State storage** | Valkey state store + Aspire | Embedded in `restate-server` (RocksDB / Bifrost log) |
| **UI** | Diagrid Dashboard (separate container; OSS Dapr has no native UI) | Built-in admin UI on `:9070` |
| **Determinism constraint** | Yes — workflow body must be deterministic for replay | Yes — workflow body must be deterministic for replay |
| **Worker model** | Long-polling activity workers connected to placement service | **No workers — Restate pushes invocations to the handler over HTTP** |
| **Killing the handler** | Workflow stalls until activity worker comes back | Restate retries with backoff; re-invokes immediately on handler restart |
| **Operational footprint** | Dapr sidecar + placement service + state store + (Diagrid Dashboard) + your app | `restate-server` binary + your Node process |
| **Wire format** | snake_case via `JsonNamingPolicy.SnakeCaseLower` | camelCase (TS native); only string *values* (`warp_core`, `red_alert`) stay snake_case |

The two most important rows for the decision-guide slide:

- **Worker model** — Dapr/Temporal/Cadence/Conductor all use long-polling workers; Restate is push-over-HTTP. This is the single biggest architectural difference among the five frameworks.
- **Operational footprint** — Dapr's runtime + state-store + dashboard topology vs Restate's one binary is the single biggest operational difference.

## 14. Out of scope (first pass)

- **Virtual Objects.** Restate's stateful-actor primitive — not needed; the scenario is single-shot run-to-completion. Mention exists in the dim-1 terminology slide.
- **Restate Cloud.** OSS server is the demo target.
- **Idempotency keys on the Express `/start`.** Not needed for stage demo (single instance, one button-press at a time).
- **Compensation / sagas.** Out of scope — the scenario doesn't need it. The decision-guide slide notes Restate ships sagas as documented patterns rather than first-class.
- **Awaitables, signals, queries, child workflows.** Restate supports promises/awakables and shared-handler queries; not needed for this scenario.
- **Authentication.** `restate-server` defaults are unauthenticated; matches the demo posture.
- **Metrics / OTLP tracing.** Stage demo only.
- **Versioning.** Restate handles workflow code changes via deployment versioning; not exercised in this 5-minute slot (Dapr's versioning story is dim-2's talking point).

## 15. Risks

| Risk | Mitigation |
|---|---|
| `restate-server` binary not on PATH or wrong version on talk laptop | Pre-talk checklist: `restate-server --version` returns the pinned version; `brew install restatedev/tap/restate-server` instructions in README. Docker image `restate/restate:1.6.x` documented as fallback. |
| Auto-registration POST hits Restate admin API before the server is healthy | Index.ts wraps the `fetch` in a small retry loop (3 attempts, 2s backoff) — Restate admin API typically up in <2s after `restate-server` start. |
| `@restatedev/restate-sdk-clients` method names drift (`workflowSendClient` / `workflowClient` / `workflowOutput`) | Pin SDK version in `package.json`; verify against `node_modules/@restatedev/restate-sdk-clients/dist/index.d.ts` after `npm ci`. Rename in `api.ts` consistently if the pinned version differs. |
| Kill of Node process during sleep doesn't visibly resume in admin UI | Confirm during dry-run: the admin UI should show the invocation transition from `RUNNING` → `SUSPENDED` (or `RETRY_BACKOFF`) → `RUNNING` as the handler reconnects. If transitions are too fast to see, slow `restate-server`'s retry backoff via `--enable-experimental-features` / config — or rely on the journal-entry timeline (entries light up green) as the visible signal. |
| Express port `3000` already taken on talk laptop | README's "before you start" step: `lsof -i :3000` to verify; switch to `3001` via env var if collision. Pre-stage check. |
| Audience asks "where's the worker?" | Pre-baked answer (and §9 narration): *"There isn't one. Restate is the worker — it calls my handler over HTTP. The SDK is just request/response glue. That's why this runs on one binary."* |
| Determinism violation in workflow body (e.g. someone copy-pastes `Math.random()` into `diagnostics.ts`) | Spec is explicit about non-determinism living in `services.ts` / `mock.ts` only. Add a one-line comment at the top of `diagnostics.ts`: `// Workflow body: deterministic. Math.random/Date.now/console.log are forbidden — use ctx.run / ctx.date / ctx.console instead.` |
| `--force` redeployment behavior changes the workflow's running invocation | Auto-registration uses `force: true` for clean restarts during edit-rebuild cycles. On stage, ensure no in-flight invocation is present at the moment of registration (talk choreography starts the workflow *after* boot is complete). Doc this as a checklist item. |
| `npm install` from a cold cache takes >60s on conference Wi-Fi | Pre-talk: run `npm ci` and commit `package-lock.json`; never `npm install` on stage. Document `npm ci --offline` for the truly paranoid path with `npm cache verify` done day-before. |
| Audience sees camelCase wire format and wonders if it's inconsistent with Conductor/Dapr snake_case | Acknowledge upfront — this is §5.0's "idiomatic naming" rule. The five-framework on-stage diff is *supposed* to show language conventions diverging. The cross-demo invariant is the **string values** (`warp_core`, `red_alert`), not field names. |

## 16. Verification checklist (before declaring the demo done)

- [ ] `restate-server --version` prints a version matching the README's pinned line.
- [ ] `npm ci` from a clean clone completes in <30s on the talk laptop's warm Node cache.
- [ ] `restate-server` boots to healthy (`curl http://localhost:9070/health` returns 200) in <5s from cold start.
- [ ] `npm run dev` starts in <3s; `index.ts` logs *"Express API listening on http://localhost:3000"* and *"Deployment registered at http://localhost:9080"*.
- [ ] In the Restate admin UI (`http://localhost:9070`), the `Services` panel shows `DiagnosticsWorkflow` (Workflow) and `activities` (Service) with handler counts 1 and 7 respectively.
- [ ] `POST /start` returns `202` with `{ "workflowId": "enterprise-ncc-1701" }`.
- [ ] The Restate admin UI's `Invocations` panel shows `DiagnosticsWorkflow/run` with id `enterprise-ncc-1701` in `RUNNING`.
- [ ] The invocation's journal panel shows 5 `Call(activities/scan*)` entries completing near-instantly, then a `Sleep(60s)` entry counting down.
- [ ] Killing the Node process at ~T+20s into the sleep (`Ctrl+C` twice or `kill -9 $(pgrep -f 'tsx src/index')`) does not terminate the invocation in the admin UI (state may briefly show `SUSPENDED` or `RETRY_BACKOFF`).
- [ ] Restarting `npm run dev` brings the handler back; the admin UI shows the invocation re-attaching; the sleep entry resumes counting from where it was (durable state survived the kill).
- [ ] After the sleep fires: journal shows `Call(activities/prioritizeIssues)` then `Call(activities/notifyBridge)`; invocation transitions to `COMPLETED`.
- [ ] The `🚨 BRIDGE ALERT — USS USS Enterprise NCC-1701-D — RED_ALERT: ... (engineer: Geordi La Forge)` line appears in the Node stdout at Warning level.
- [ ] `GET /status/enterprise-ncc-1701` after completion returns `{ "status": "COMPLETED", "output": { ..., "bridgeNotification": "🚨 BRIDGE ALERT ..." } }`.
- [ ] Run the workflow 10× in a row — `bridgeNotification` is never null (rigging holds; `red_alert` fires every time).
- [ ] `npm run check` passes with `--strict` clean (no `any`, no `@ts-ignore`).
- [ ] Total wall-clock from `restate-server` start to a completed workflow: under 90 seconds (5s server start + 3s node start + 5s scans + 60s sleep + 2s prioritize+notify + buffer).
- [ ] **Sister-demo check (vs Dapr):** open `demos/05-restate/src/diagnostics.ts` and `demos/04-dapr/EnterpriseDiagnostics.ApiService/Workflows/DiagnosticsWorkflow.cs` side-by-side. The shape — fan-out, sleep, conditional notify — is recognizably the same algorithm in two languages. The audience can flip between them and see the model is the same (workflow-as-code) while the operational footprint and worker model are inverted.
- [ ] **Footprint check:** `ls demos/05-restate/` and `find demos/05-restate -name node_modules -prune -o -type f -print | wc -l` reveal a notably smaller surface than the other four demos. The single-binary + small-package-tree fact is the dimension-3 (Hosting) callback the talk is set up to make.
