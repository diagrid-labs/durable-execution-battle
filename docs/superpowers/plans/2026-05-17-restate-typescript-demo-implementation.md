# Restate (TypeScript) Demo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Git policy for this plan:** The user handles all git operations manually. Do NOT run `git add`, `git commit`, `git push`, `git stash`, or any other git command. Each task ends with a *verification* step instead of a commit. All work happens on `main`.

**Goal:** Build the USS Enterprise diagnostics workflow demo in **Restate + TypeScript** under `demos/05-restate/`, matching the canonical scenario (`demos/shared/scenario.md`) and the cross-demo wire invariants (`plan.md` §5.0), as the final framework demo (`plan.md` §1, t=38:00) of the *Durable Execution Battle* talk.

**Architecture:** Single Node 22 process that (a) binds a `restate.workflow` (`DiagnosticsWorkflow`) and a sibling `restate.service` (`activities`, with 7 handlers) to the Restate SDK endpoint on port `9080`, (b) starts a thin Express wrapper on port `3000` exposing `POST /start` and `GET /status/:id` so the audience sees the same `.http` file shape as the other four demos, and (c) auto-registers the deployment with Restate's admin API at startup. State lives entirely inside the `restate-server` binary (RocksDB / Bifrost log) — **no Postgres, no Kafka, no Cassandra, no Docker required for Restate itself**. The workflow body uses `Promise.all` over typed service-client calls for fan-out, `ctx.sleep({ seconds: 60 })` for the durable kill point, and a conditional branch for the bridge notification. All non-determinism (RNG, console output) lives inside the handler implementations of `activities`, never inside the workflow body.

**Tech Stack:** Node 22 LTS · TypeScript 5.7 · `@restatedev/restate-sdk` ^1.6 · `@restatedev/restate-sdk-clients` ^1.6 · Express ^4.21 · `tsx` (dev runner, no compile step on stage) · `node:test` + `node:assert` for unit tests (zero added test dependencies — preserves the small-dep pitch the demo is built to make).

**Reference (scenario only, not style):** `demos/04-dapr/EnterpriseDiagnostics.ApiService/` is the canonical implementation. Match activity names (5 × `Scan*`, `PrioritizeIssues`, `NotifyBridge`), severity strings (`green` / `yellow_alert` / `red_alert`), subsystem id strings (`warp_core` / `shields` / `life_support` / `sensors` / `weapons`), health range `[30..65]`, severity thresholds (`<50 → red_alert`, `<75 → yellow_alert`, else `green`), bridge message format, and workflow id `enterprise-ncc-1701`. Do **not** transliterate C# style — TypeScript field names are camelCase per `plan.md` §5.0; only string *values* on the wire stay snake_case.

**Design spec (locked):** [`docs/superpowers/specs/2026-05-17-restate-typescript-demo-design.md`](../specs/2026-05-17-restate-typescript-demo-design.md). The spec is the contract; if a step here disagrees with it, treat that as a bug in this plan and stop to ask.

---

## File Structure

```
demos/05-restate/
├── README.md
├── package.json
├── package-lock.json                # committed; produced by `npm install`
├── tsconfig.json
├── .gitignore
├── enterprise-diagnostics.http      # VS Code REST Client / IntelliJ HTTP Client
│
└── src/
    ├── index.ts                     # entry: binds Restate endpoint + starts Express + auto-registers deployment
    ├── api.ts                       # Express wrapper: POST /start, GET /status/:id
    ├── diagnostics.ts               # restate.workflow — the `run` handler
    ├── services.ts                  # restate.service "activities" with 7 handlers
    ├── mock.ts                      # generateScan + prioritize (the only place Math.random lives)
    ├── models.ts                    # shared TS types
    │
    └── __tests__/                   # node:test files (auto-discovered by `node --test`)
        ├── mock.test.ts             # generateScan invariants + prioritize threshold table
        └── notify.test.ts           # bridge alert message format
```

Single responsibility per file. Tests live next to source under `src/__tests__/` so the test runner picks them up via `node --test --import tsx 'src/**/*.test.ts'`. No `dist/` checked in — `tsx` runs `.ts` directly in dev; `npm run check` is the type-only gate.

**Structural delta vs the canonical Dapr reference** (see spec §3 diagram):

- The entire Dapr `Activities/` folder (5 scan files + `MockAnalysisGenerator.cs` + `PrioritizeIssuesActivity.cs` + `NotifyBridgeActivity.cs`) collapses into **one** TypeScript file (`src/services.ts`) with 7 inline handler functions on a single `restate.service`.
- Aspire AppHost + ServiceDefaults + Valkey + Dapr sidecar + Diagrid Dashboard collapse to **one** binary (`restate-server`) plus this Node process.
- That collapse *is* the on-stage talking point — preserve it. Do not split `services.ts` into one-file-per-handler.

---

## Task 1: Project skeleton — `package.json`, `tsconfig.json`, `.gitignore`

**Files:**
- Create: `demos/05-restate/package.json`
- Create: `demos/05-restate/tsconfig.json`
- Create: `demos/05-restate/.gitignore`

- [ ] **Step 1: Create `demos/05-restate/package.json`**

```json
{
  "name": "enterprise-diagnostics-restate",
  "version": "0.1.0",
  "private": true,
  "type": "module",
  "scripts": {
    "dev": "tsx watch src/index.ts",
    "start": "tsx src/index.ts",
    "check": "tsc --noEmit",
    "test": "node --test --import tsx 'src/**/*.test.ts'"
  },
  "dependencies": {
    "@restatedev/restate-sdk": "^1.6.0",
    "@restatedev/restate-sdk-clients": "^1.6.0",
    "express": "^4.21.0"
  },
  "devDependencies": {
    "@types/express": "^5.0.0",
    "@types/node": "^22.0.0",
    "tsx": "^4.19.0",
    "typescript": "^5.7.0"
  },
  "engines": {
    "node": ">=22.0.0"
  }
}
```

Why these and only these: the spec §10 locks the dependency footprint at 7 total (3 runtime + 4 dev) to preserve the "small-footprint pitch" — adding anything else (vitest, jest, dotenv, axios) breaks the visual proof. Tests use the Node 22 built-in `node:test` runner; `tsx` provides the loader.

- [ ] **Step 2: Create `demos/05-restate/tsconfig.json`**

```json
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
    "allowImportingTsExtensions": true,
    "noEmit": true
  },
  "include": ["src/**/*"]
}
```

Three things to call out:
- `noUncheckedIndexedAccess` is on per spec §11; it forces explicit handling of `scans[0]` access in `mock.ts` so a future copy-paste doesn't silently return `undefined`.
- `allowImportingTsExtensions: true` lets source files import each other using `.ts` extensions (`import { ... } from "./mock.ts"`), which `tsx` resolves at runtime and keeps the imports grep-friendly. Requires `noEmit: true` (set above) — we never produce `.js` artifacts; `tsx` runs `.ts` directly.
- `outDir` is intentionally absent — we never emit, so there's no `dist/` to write.

- [ ] **Step 3: Create `demos/05-restate/.gitignore`**

```
node_modules/
dist/
.DS_Store
*.log
.env
.env.local
```

- [ ] **Step 4: Install dependencies**

Run from the repo root:
```bash
cd demos/05-restate && npm install
```
Expected: `npm install` completes; `node_modules/` and `package-lock.json` appear. No vulnerabilities reported above moderate (npm audit will run automatically).

- [ ] **Step 5: Verify type-check works on an empty project**

Add a placeholder `src/index.ts` containing only `export {};` so `tsc` has something to check, then:
```bash
cd demos/05-restate && npm run check
```
Expected: exits 0, no output.

- [ ] **Step 6: Verify the test runner is wired up**

Add a placeholder `src/__tests__/sanity.test.ts`:
```typescript
import { test } from "node:test";
import assert from "node:assert/strict";

test("sanity", () => {
  assert.equal(1 + 1, 2);
});
```
Run:
```bash
cd demos/05-restate && npm test
```
Expected: 1 test, 1 pass. Then **delete** `src/__tests__/sanity.test.ts` and remove the placeholder content from `src/index.ts` (the file is fully written in Task 7).

---

## Task 2: Model types — `src/models.ts`

**Files:**
- Create: `demos/05-restate/src/models.ts`

- [ ] **Step 1: Create `src/models.ts` with the full type set**

```typescript
// src/models.ts
//
// Wire-format invariants from plan.md §5.0 hold across all five demos:
//   - subsystem string values: warp_core, shields, life_support, sensors, weapons
//   - severity string values:  green, yellow_alert, red_alert
//   - severity thresholds:     min(health) < 50 → red_alert, < 75 → yellow_alert, else green
//   - health range:            [30..65] inclusive (rigged so red_alert is overwhelmingly likely)
//   - bridge alert format:     🚨 BRIDGE ALERT — USS {shipName} — {SEVERITY}: {summary} (engineer: {engineerName})
// TypeScript field names are camelCase per plan.md §5.0; only the *values* above stay snake_case.

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
  id: string;
  shipName: string;
  diagnosticsDate: string;
  engineerName: string;
}

export interface DiagnosticsOutput {
  shipName: string;
  diagnosticsDate: string;
  engineerName: string;
  priority: Priority;
  bridgeNotification: string | null;
}

export interface ScanInput {
  shipName: string;
  subsystem: SubsystemId;
}

export interface ScanResult {
  subsystem: SubsystemId;
  health: number;
  anomalies: string[];
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

`Severity` is a string-literal union (not a TS `enum`) so the on-wire values match `green` / `yellow_alert` / `red_alert` with zero enum-to-string mapping.

- [ ] **Step 2: Type-check the models compile**

```bash
cd demos/05-restate && npm run check
```
Expected: exits 0.

---

## Task 3: Mock analysis generator with TDD — `src/mock.ts`

This is the only place `Math.random` lives. Workflow body must stay deterministic, so the RNG MUST stay inside this module (called from `services.ts` handlers).

**Files:**
- Create: `demos/05-restate/src/__tests__/mock.test.ts`
- Create: `demos/05-restate/src/mock.ts`

- [ ] **Step 1: Write failing test for `generateScan` invariants**

Create `src/__tests__/mock.test.ts`:

```typescript
import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { generateScan, prioritize } from "../mock.ts";
import type { ScanResult } from "../models.ts";

describe("generateScan", () => {
  const candidates = ["alpha", "beta", "gamma", "delta", "epsilon"];

  it("returns the requested subsystem id verbatim", () => {
    const result = generateScan("warp_core", candidates);
    assert.equal(result.subsystem, "warp_core");
  });

  it("returns health in the rigged range [30, 65] inclusive", () => {
    for (let i = 0; i < 200; i++) {
      const result = generateScan("warp_core", candidates);
      assert.ok(result.health >= 30, `health ${result.health} < 30`);
      assert.ok(result.health <= 65, `health ${result.health} > 65`);
    }
  });

  it("returns 1 to 3 anomalies sampled from the candidate list", () => {
    for (let i = 0; i < 200; i++) {
      const result = generateScan("shields", candidates);
      assert.ok(result.anomalies.length >= 1, "must have at least 1 anomaly");
      assert.ok(result.anomalies.length <= 3, "must have at most 3 anomalies");
      for (const a of result.anomalies) {
        assert.ok(candidates.includes(a), `unexpected anomaly: ${a}`);
      }
    }
  });

  it("returns distinct anomalies (no duplicates in a single scan)", () => {
    for (let i = 0; i < 200; i++) {
      const result = generateScan("sensors", candidates);
      const unique = new Set(result.anomalies);
      assert.equal(unique.size, result.anomalies.length);
    }
  });
});

describe("prioritize", () => {
  function scan(subsystem: ScanResult["subsystem"], health: number): ScanResult {
    return { subsystem, health, anomalies: [] };
  }

  it("returns red_alert when the worst scan health is below 50", () => {
    const result = prioritize([
      scan("warp_core", 49),
      scan("shields", 80),
      scan("life_support", 90),
      scan("sensors", 70),
      scan("weapons", 60),
    ]);
    assert.equal(result.severity, "red_alert");
  });

  it("returns yellow_alert when the worst scan health is in [50, 75)", () => {
    const result = prioritize([
      scan("warp_core", 74),
      scan("shields", 80),
      scan("life_support", 90),
      scan("sensors", 76),
      scan("weapons", 85),
    ]);
    assert.equal(result.severity, "yellow_alert");
  });

  it("returns yellow_alert exactly at the lower edge of the band (50)", () => {
    const result = prioritize([scan("warp_core", 50)]);
    assert.equal(result.severity, "yellow_alert");
  });

  it("returns green when all scans are at or above 75", () => {
    const result = prioritize([
      scan("warp_core", 75),
      scan("shields", 95),
      scan("life_support", 88),
      scan("sensors", 80),
      scan("weapons", 90),
    ]);
    assert.equal(result.severity, "green");
  });

  it("names the worst subsystem in the summary", () => {
    const result = prioritize([
      scan("warp_core", 80),
      scan("shields", 32),
      scan("life_support", 90),
      scan("sensors", 76),
      scan("weapons", 60),
    ]);
    assert.match(result.summary, /shields/);
    assert.match(result.summary, /32/);
  });

  it("counts subsystems below the operational threshold (<75) in the summary", () => {
    const result = prioritize([
      scan("warp_core", 40),
      scan("shields", 70),
      scan("life_support", 90),
      scan("sensors", 60),
      scan("weapons", 80),
    ]);
    // worst=40, three subsystems below 75 (40, 70, 60)
    assert.match(result.summary, /3/);
  });
});
```

- [ ] **Step 2: Run the tests — verify they fail because `mock.ts` doesn't exist yet**

```bash
cd demos/05-restate && npm test
```
Expected: failure, module `../mock.ts` cannot be found.

- [ ] **Step 3: Create `src/mock.ts` with the minimal implementation**

```typescript
// src/mock.ts
//
// Non-determinism lives here, not in the workflow body. The workflow's `run`
// handler is replayed by Restate from the journal; Math.random() inside it
// would break replay. These functions are called only from `services.ts`
// handlers, which are journaled call-by-call — Restate stores the *return
// value*, so random results are stable across replay.

import type { ScanResult, Priority, Severity, SubsystemId } from "./models.ts";

export function generateScan(
  subsystem: SubsystemId,
  candidates: string[],
): ScanResult {
  // Biased low: health ∈ [30, 65] so min(health) < 50 is overwhelmingly likely.
  const health = 30 + Math.floor(Math.random() * 36);

  // 1..3 anomalies, sampled without replacement from the candidate list.
  const count = 1 + Math.floor(Math.random() * 3);
  const shuffled = [...candidates].sort(() => Math.random() - 0.5);

  return {
    subsystem,
    health,
    anomalies: shuffled.slice(0, count),
  };
}

export function prioritize(scans: ScanResult[]): Priority {
  if (scans.length === 0) {
    return { severity: "green", summary: "no scans submitted" };
  }

  // noUncheckedIndexedAccess: scans[0] is ScanResult | undefined, but we
  // just guarded length > 0. The `!` is the explicit narrowing for the
  // type-checker; the runtime guard above is the real safety net.
  let worst: ScanResult = scans[0]!;
  for (const s of scans) {
    if (s.health < worst.health) worst = s;
  }

  const severity: Severity =
    worst.health < 50 ? "red_alert" :
    worst.health < 75 ? "yellow_alert" :
                        "green";

  const belowThreshold = scans.filter((s) => s.health < 75).length;
  const summary =
    `${worst.subsystem} critical at ${worst.health}% health; ` +
    `${belowThreshold} subsystem(s) below threshold`;

  return { severity, summary };
}
```

- [ ] **Step 4: Run the tests — verify they pass**

```bash
cd demos/05-restate && npm test
```
Expected: all `generateScan` + `prioritize` tests pass.

- [ ] **Step 5: Type-check**

```bash
cd demos/05-restate && npm run check
```
Expected: exits 0.

- [ ] **Step 6: Verification step — confirm RNG isolation**

Run:
```bash
cd demos/05-restate && grep -n "Math\.random\|Date\.now" src/*.ts || echo "no matches outside __tests__"
```
Expected: `Math.random` appears only in `src/mock.ts`. If it shows up in `src/diagnostics.ts` or `src/services.ts` later, the workflow's determinism guarantee is broken — fix immediately.

---

## Task 4: Activities service — `src/services.ts`

One `restate.service` with 7 handlers. Each handler appears as a separate journal entry (`activities/scanWarpCore`, `activities/notifyBridge`, etc.) in the Restate admin UI — that's the on-stage readability point.

**Files:**
- Create: `demos/05-restate/src/__tests__/notify.test.ts`
- Create: `demos/05-restate/src/services.ts`

- [ ] **Step 1: Write a failing test for the bridge alert message format**

Create `src/__tests__/notify.test.ts`:

```typescript
import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { formatBridgeAlert } from "../services.ts";

describe("formatBridgeAlert", () => {
  it("matches the canonical cross-demo format exactly", () => {
    const message = formatBridgeAlert({
      shipName: "USS Enterprise NCC-1701-D",
      engineerName: "Geordi La Forge",
      priority: {
        severity: "red_alert",
        summary: "warp_core critical at 32% health; 4 subsystem(s) below threshold",
      },
    });

    assert.equal(
      message,
      "🚨 BRIDGE ALERT — USS USS Enterprise NCC-1701-D — RED_ALERT: " +
        "warp_core critical at 32% health; 4 subsystem(s) below threshold " +
        "(engineer: Geordi La Forge)",
    );
  });

  it("uppercases the severity wire value", () => {
    const message = formatBridgeAlert({
      shipName: "X",
      engineerName: "Y",
      priority: { severity: "yellow_alert", summary: "Z" },
    });
    assert.match(message, /YELLOW_ALERT/);
  });
});
```

Note: the canonical format from `plan.md` §5.0 is `🚨 BRIDGE ALERT — USS {shipName} — ...`. The literal `USS ` prefix is baked into the template; if the input `shipName` already starts with `USS` (as it will in the demo), the output reads `USS USS Enterprise NCC-1701-D` — that's the consistent template across all five demos, *not* a bug. The test pins that exact shape.

- [ ] **Step 2: Run the test — verify it fails because `formatBridgeAlert` is not exported yet**

```bash
cd demos/05-restate && npm test
```
Expected: failure — `formatBridgeAlert` not found in `../services.ts`.

- [ ] **Step 3: Create `src/services.ts`**

```typescript
// src/services.ts
//
// One Restate service named "activities" hosting all 7 handlers. Each handler
// is journaled as its own entry in the Restate UI; the workflow calls them
// via a typed service client (see diagnostics.ts).
//
// Non-determinism is allowed here — these functions run only when Restate
// invokes them, never during replay. The workflow stores their return values
// in the journal, so Math.random / Date.now produce stable replay results.

import * as restate from "@restatedev/restate-sdk";
import { generateScan, prioritize } from "./mock.ts";
import type {
  ScanInput,
  ScanResult,
  PrioritizeInput,
  Priority,
  BridgeNotificationInput,
  BridgeNotificationResult,
} from "./models.ts";

// ── Per-subsystem candidate anomaly lists (sampled by generateScan) ──

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

// Exported for unit testing — keeps the alert template in one place and
// lets tests pin the exact cross-demo format without booting Restate.
export function formatBridgeAlert(input: BridgeNotificationInput): string {
  return (
    `🚨 BRIDGE ALERT — USS ${input.shipName} — ` +
    `${input.priority.severity.toUpperCase()}: ${input.priority.summary} ` +
    `(engineer: ${input.engineerName})`
  );
}

export const activities = restate.service({
  name: "activities",
  handlers: {
    scanWarpCore: async (
      _ctx: restate.Context,
      _input: ScanInput,
    ): Promise<ScanResult> => generateScan("warp_core", warpCoreCandidates),

    scanShields: async (
      _ctx: restate.Context,
      _input: ScanInput,
    ): Promise<ScanResult> => generateScan("shields", shieldsCandidates),

    scanLifeSupport: async (
      _ctx: restate.Context,
      _input: ScanInput,
    ): Promise<ScanResult> => generateScan("life_support", lifeSupportCandidates),

    scanSensors: async (
      _ctx: restate.Context,
      _input: ScanInput,
    ): Promise<ScanResult> => generateScan("sensors", sensorsCandidates),

    scanWeapons: async (
      _ctx: restate.Context,
      _input: ScanInput,
    ): Promise<ScanResult> => generateScan("weapons", weaponsCandidates),

    prioritizeIssues: async (
      _ctx: restate.Context,
      input: PrioritizeInput,
    ): Promise<Priority> => prioritize(input.scans),

    notifyBridge: async (
      _ctx: restate.Context,
      input: BridgeNotificationInput,
    ): Promise<BridgeNotificationResult> => {
      const message = formatBridgeAlert(input);
      console.warn(message);
      return { message };
    },
  },
});
```

- [ ] **Step 4: Run the tests — verify they pass**

```bash
cd demos/05-restate && npm test
```
Expected: `formatBridgeAlert` tests pass; `mock.ts` tests still pass.

- [ ] **Step 5: Type-check**

```bash
cd demos/05-restate && npm run check
```
Expected: exits 0.

- [ ] **Step 6: Verification step — handler count**

Run:
```bash
cd demos/05-restate && grep -E "^\s+(scan|prioritize|notify)" src/services.ts | wc -l
```
Expected: `7` (five `scan*` + `prioritizeIssues` + `notifyBridge`).

---

## Task 5: Workflow definition — `src/diagnostics.ts`

The file the audience sees first on stage. The body must be deterministic — only Restate-journaled primitives (service-client calls, `ctx.sleep`, `ctx.console`) are allowed.

**Files:**
- Create: `demos/05-restate/src/diagnostics.ts`

- [ ] **Step 1: Create `src/diagnostics.ts`**

```typescript
// src/diagnostics.ts
//
// Workflow body MUST be deterministic. Restate replays this handler on every
// re-invocation (e.g. after a kill -9 of the Node process during the 60s
// sleep). Forbidden inside this function: Math.random, Date.now, plain
// console.log, file/network I/O, any non-journaled side-effect.
// Allowed: service-client calls (each is one journal entry), ctx.sleep,
// ctx.console, ctx.run.

import * as restate from "@restatedev/restate-sdk";
import { activities } from "./services.ts";
import type {
  DiagnosticsInput,
  DiagnosticsOutput,
  ScanResult,
  Priority,
} from "./models.ts";

export const diagnosticsWorkflow = restate.workflow({
  name: "DiagnosticsWorkflow",
  handlers: {
    run: async (
      ctx: restate.WorkflowContext,
      input: DiagnosticsInput,
    ): Promise<DiagnosticsOutput> => {
      const client = ctx.serviceClient(activities);

      ctx.console.log(
        `Starting diagnostics for ${input.shipName} (id: ${input.id})`,
      );

      // ── Fan-out: 5 parallel subsystem scans ──
      const scans: ScanResult[] = await Promise.all([
        client.scanWarpCore({ shipName: input.shipName, subsystem: "warp_core" }),
        client.scanShields({ shipName: input.shipName, subsystem: "shields" }),
        client.scanLifeSupport({ shipName: input.shipName, subsystem: "life_support" }),
        client.scanSensors({ shipName: input.shipName, subsystem: "sensors" }),
        client.scanWeapons({ shipName: input.shipName, subsystem: "weapons" }),
      ]);

      ctx.console.log("All scans complete — engineering review pending, sleeping 60s");

      // ── 60s durable sleep — ⚡ KILL POINT ⚡ ──
      await ctx.sleep({ seconds: 60 });

      // ── Prioritization ──
      const priority: Priority = await client.prioritizeIssues({
        shipName: input.shipName,
        scans,
      });

      ctx.console.log(`Prioritized with severity: ${priority.severity}`);

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

- [ ] **Step 2: Type-check**

```bash
cd demos/05-restate && npm run check
```
Expected: exits 0. If the SDK's `WorkflowContext` doesn't expose `serviceClient` or `sleep` with these signatures in the pinned version, see the SDK-shape note in Task 7 Step 6 — adjust the call shape consistently but keep the workflow algorithm identical.

- [ ] **Step 3: Verification step — determinism scan**

```bash
cd demos/05-restate && grep -nE "Math\.random|Date\.now|setTimeout|console\.(log|warn|error)" src/diagnostics.ts || echo "clean"
```
Expected: prints `clean`. Any match here means the workflow body has non-journaled side-effects and replay will misbehave — fix immediately.

---

## Task 6: Express API wrapper — `src/api.ts`

Mirrors the `POST /start` / `GET /status/:id` surface the other four demos expose, so the on-stage `.http` file shape is symmetric.

**Files:**
- Create: `demos/05-restate/src/api.ts`

- [ ] **Step 1: Create `src/api.ts`**

```typescript
// src/api.ts
//
// Thin Express wrapper. The audience POSTs JSON here; we forward to Restate's
// ingress (default :8080) using the typed clients SDK. No durable logic
// lives in this file — it's pure glue so the on-stage .http file shape
// matches Cadence/Temporal/Conductor/Dapr.

import express from "express";
import * as clients from "@restatedev/restate-sdk-clients";
import { diagnosticsWorkflow } from "./diagnostics.ts";
import type { DiagnosticsInput } from "./models.ts";

const RESTATE_INGRESS = process.env.RESTATE_INGRESS ?? "http://localhost:8080";
const PORT = Number(process.env.PORT ?? 3000);

export function buildApi() {
  const app = express();
  app.use(express.json());

  const restateClient = clients.connect({ url: RESTATE_INGRESS });

  // POST /start — kick off a diagnostics workflow.
  // Returns 202 immediately with the workflow id; the actual run takes 60s+.
  app.post("/start", async (req, res) => {
    const input = req.body as DiagnosticsInput;
    if (!input?.id || !input?.shipName) {
      res.status(400).json({ error: "id and shipName are required" });
      return;
    }
    try {
      const handle = restateClient.workflowSendClient(diagnosticsWorkflow, input.id);
      await handle.run(input);
      res.status(202).json({ workflowId: input.id });
    } catch (err) {
      console.error("Failed to start workflow", err);
      res.status(500).json({ error: String(err) });
    }
  });

  // GET /status/:id — if the workflow has completed, return its output;
  // otherwise return 202 with {status: "RUNNING"}.
  app.get("/status/:id", async (req, res) => {
    const id = req.params.id;
    try {
      const handle = restateClient.workflowClient(diagnosticsWorkflow, id);
      const output = await Promise.race([
        handle.workflowOutput(),
        new Promise<never>((_, rej) =>
          setTimeout(() => rej(new Error("pending")), 250),
        ),
      ]);
      res.json({ status: "COMPLETED", output });
    } catch {
      res.status(202).json({ status: "RUNNING" });
    }
  });

  return app.listen(PORT, () =>
    console.log(`Express API listening on http://localhost:${PORT}`),
  );
}
```

> **SDK-shape note (spec §7):** the `@restatedev/restate-sdk-clients` method names (`workflowSendClient` / `workflowClient` / `workflowOutput`) target SDK v1.x. After `npm install`, verify the exact names by inspecting `node_modules/@restatedev/restate-sdk-clients/dist/*.d.ts` (or running `npm run check`). If the pinned version uses `attach()` / `result()` / a different signature, rename consistently here — the Express surface (`POST /start`, `GET /status/:id`) stays the same.

- [ ] **Step 2: Type-check**

```bash
cd demos/05-restate && npm run check
```
Expected: exits 0. If the SDK exposes different method names, the type-checker will flag them — adjust per the note above and re-run.

- [ ] **Step 3: Verification step — Express API surface**

```bash
cd demos/05-restate && grep -nE "app\.(get|post|put|delete)" src/api.ts
```
Expected: exactly two routes — `POST /start` and `GET /status/:id`. No other routes on this wrapper (`PUT` / `DELETE` / `/terminate` / etc. are explicitly out of scope per spec §14).

---

## Task 7: Entry point with auto-registration — `src/index.ts`

One process: binds the Restate handler endpoint on `:9080`, starts the Express wrapper on `:3000`, and POSTs to Restate's admin API (`:9070/deployments`) so the audience doesn't run `restate deployments register` by hand.

**Files:**
- Create: `demos/05-restate/src/index.ts` (replacing the placeholder from Task 1)

- [ ] **Step 1: Replace `src/index.ts` with the real entry point**

```typescript
// src/index.ts
//
// Single Node process:
//   1. Restate SDK endpoint (HTTP/2) bound on :9080 — Restate pushes
//      invocations here.
//   2. Express wrapper (HTTP/1.1) on :3000 — audience POSTs /start.
//   3. Auto-registers the deployment with restate-server's admin API
//      (default :9070) so no manual `restate deployments register` step.
//
// One `npm run dev`; one `kill -9` brings both down — that's the durability
// demo's setup.

import * as restate from "@restatedev/restate-sdk";
import { diagnosticsWorkflow } from "./diagnostics.ts";
import { activities } from "./services.ts";
import { buildApi } from "./api.ts";

const HANDLER_PORT = Number(process.env.HANDLER_PORT ?? 9080);
const ADMIN_URL = process.env.RESTATE_ADMIN ?? "http://localhost:9070";

// 1. Restate handler endpoint.
restate
  .endpoint()
  .bind(diagnosticsWorkflow)
  .bind(activities)
  .listen(HANDLER_PORT);

// 2. Express wrapper.
buildApi();

// 3. Auto-register the deployment with Restate. force=true lets the
//    registrar replace the existing deployment at this URI without manual
//    cleanup across edit-rebuild cycles.
void (async () => {
  const uri = `http://localhost:${HANDLER_PORT}`;
  const maxAttempts = 5;
  const backoffMs = 2000;

  for (let attempt = 1; attempt <= maxAttempts; attempt++) {
    try {
      const res = await fetch(`${ADMIN_URL}/deployments`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ uri, force: true }),
      });
      if (res.ok || res.status === 409) {
        console.log(`Deployment registered at ${uri}`);
        return;
      }
      console.error(
        `Deployment registration attempt ${attempt} failed: ${res.status} ${await res.text()}`,
      );
    } catch (err) {
      console.error(`Deployment registration attempt ${attempt} threw: ${err}`);
    }
    if (attempt < maxAttempts) {
      await new Promise((r) => setTimeout(r, backoffMs));
    }
  }

  console.error(
    `Deployment registration failed after ${maxAttempts} attempts. ` +
      `Is restate-server running at ${ADMIN_URL}?`,
  );
  process.exit(1);
})();
```

The retry loop addresses spec §15's risk: the Node process can start before `restate-server`'s admin API is ready. 5 × 2 s = up to 10 s wall-clock — enough for any normal start.

- [ ] **Step 2: Type-check**

```bash
cd demos/05-restate && npm run check
```
Expected: exits 0.

- [ ] **Step 3: Smoke-boot the Node process (no Restate server yet)**

```bash
cd demos/05-restate && npm run dev
```
Expected within 3 seconds:
- `Express API listening on http://localhost:3000` appears in stdout.
- The Restate SDK prints a banner indicating the handler endpoint is listening on `:9080` (the exact line depends on SDK version; the audience-visible signal is that the process doesn't exit).
- The auto-registration loop prints `Deployment registration attempt 1 failed:` (or similar) once per retry attempt because `restate-server` is not running.
- After ~10 seconds the process exits with `Deployment registration failed after 5 attempts. Is restate-server running at http://localhost:9070?` and a non-zero exit code.

Kill the process with Ctrl+C if it's still attempting; the failure mode is expected and confirms the retry loop works.

- [ ] **Step 4: Install restate-server (one-time, if not already installed)**

macOS (preferred for the talk laptop per spec §2):
```bash
brew install restatedev/tap/restate-server
restate-server --version
```
Expected: prints a version (target: `1.6.x` or newer; pin the exact version pre-talk per spec §15).

Linux / Windows / fallback: use the Docker image `restate/restate:1.6.x` documented in the README in Task 9.

- [ ] **Step 5: Boot restate-server and re-run the Node process — verify deployment registration succeeds**

Terminal 1:
```bash
restate-server
```
Wait ~5 seconds; verify it's up:
```bash
curl -fsS http://localhost:9070/health && echo " ok"
```
Expected: `ok` (or whatever the SDK health endpoint returns with HTTP 200).

Terminal 2:
```bash
cd demos/05-restate && npm run dev
```
Expected within 5 seconds:
- `Express API listening on http://localhost:3000`
- `Deployment registered at http://localhost:9080`
- No retries needed (or at most one retry).

- [ ] **Step 6: Verify services show up in the Restate admin UI**

Open `http://localhost:9070` in a browser → **Services** panel.

Expected:
- A workflow named `DiagnosticsWorkflow` with handler count 1 (`run`).
- A service named `activities` with handler count 7 (`scanWarpCore`, `scanShields`, `scanLifeSupport`, `scanSensors`, `scanWeapons`, `prioritizeIssues`, `notifyBridge`).

If either is missing, the `bind()` calls in `index.ts` are wrong or the SDK version's binding API differs — investigate before proceeding. If handler counts are off, an `_input` parameter was renamed or a handler key is misspelled — cross-check against `services.ts`.

Kill both processes with Ctrl+C before Task 8.

---

## Task 8: HTTP test file — `enterprise-diagnostics.http`

**Files:**
- Create: `demos/05-restate/enterprise-diagnostics.http`

- [ ] **Step 1: Create `enterprise-diagnostics.http`**

```http
### Start a diagnostics workflow
POST http://localhost:3000/start
Content-Type: application/json

{
  "id": "enterprise-ncc-1701",
  "shipName": "USS Enterprise NCC-1701-D",
  "diagnosticsDate": "2370-04-08",
  "engineerName": "Geordi La Forge"
}

### Query workflow status
GET http://localhost:3000/status/enterprise-ncc-1701
```

Wire body is camelCase per `plan.md` §5.0 ("each demo uses its language's native conventions") — that's the visible delta from the Dapr/Conductor `.http` files and is intentional.

- [ ] **Step 2: Verification step — confirm the workflow id is the canonical one**

```bash
cd demos/05-restate && grep -n "enterprise-ncc-1701" enterprise-diagnostics.http
```
Expected: 2 matches (one in the POST body, one in the GET URL).

---

## Task 9: README — `demos/05-restate/README.md`

Mirrors the structure of the sister demos' READMEs so the audience can flip between them.

**Files:**
- Create: `demos/05-restate/README.md`

- [ ] **Step 1: Create `demos/05-restate/README.md`**

````markdown
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
restate-server

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
- SDK method names (`workflowSendClient` / `workflowClient` / `workflowOutput`) match `node_modules/@restatedev/restate-sdk-clients/dist/*.d.ts` for the pinned version. If not, `src/api.ts` is the one place to update.
- Full end-to-end dry run including the kill-and-resume sequence on the talk laptop, twice in a row.
- `npm test` passes from a clean checkout.
- Ports `3000`, `8080`, `9070`, `9080` are all free (`lsof -i :3000 -i :8080 -i :9070 -i :9080`).
````

- [ ] **Step 2: Verification step — README mentions all four ports**

```bash
cd demos/05-restate && grep -cE "3000|8080|9070|9080" README.md
```
Expected: at least 4 matches.

---

## Task 10: End-to-end verification — kill-and-resume

This is the dress rehearsal. Run the full audience-facing flow on the talk laptop (or the closest equivalent dev box) twice. If anything is fragile, fix it now — there is no on-stage redo.

**Files:** none modified — purely verification.

- [ ] **Step 1: Pre-flight — ports free, fresh state**

```bash
lsof -i :3000 -i :8080 -i :9070 -i :9080
```
Expected: no output (all four ports free). If any port is taken, kill the holder or change env vars before continuing.

If `restate-server` was run previously and left state behind, optionally wipe it (Restate's default data dir is `restate-data/` next to the binary's working directory):
```bash
rm -rf restate-data/
```

- [ ] **Step 2: Boot `restate-server` (Terminal 1)**

```bash
restate-server
```
Wait until you see the server log "ingress" listening on `:8080` and "admin" on `:9070`. Verify:
```bash
curl -fsS http://localhost:9070/health && echo " healthy"
```
Expected: `healthy`.

- [ ] **Step 3: Boot the Node process (Terminal 2)**

```bash
cd demos/05-restate && npm run dev
```
Expected within 5 seconds:
- `Express API listening on http://localhost:3000`
- `Deployment registered at http://localhost:9080`

Verify in the Restate UI (`http://localhost:9070` → **Services**) that `DiagnosticsWorkflow` (1 handler) and `activities` (7 handlers) are listed.

- [ ] **Step 4: Start the workflow (Terminal 3)**

```bash
curl -X POST http://localhost:3000/start \
  -H 'Content-Type: application/json' \
  -d '{"id":"enterprise-ncc-1701","shipName":"USS Enterprise NCC-1701-D","diagnosticsDate":"2370-04-08","engineerName":"Geordi La Forge"}'
```
Expected: HTTP 202 with `{"workflowId":"enterprise-ncc-1701"}`.

In the Restate UI's **Invocations** panel, `DiagnosticsWorkflow/run` with id `enterprise-ncc-1701` appears in state `RUNNING`. The journal panel fills with 5 `Call(activities/scan*)` entries (completing near-instantly), then a `Sleep(60s)` entry counting down.

- [ ] **Step 5: Kill the Node process at ~T+20 s into the sleep**

After ~20 seconds (sleep counter shows ~40 s remaining), in Terminal 2:
```bash
# Two Ctrl+C presses, OR:
kill -9 $(pgrep -f 'tsx src/index')
```

Expected in the Restate UI:
- The invocation does **not** transition to `FAILED`. The state may briefly show `SUSPENDED` or `RETRY_BACKOFF` as Restate detects the handler is gone.
- The journal entries already completed (5 scans + sleep) remain visible. The durable state lives in the server.

- [ ] **Step 6: Restart the Node process**

In Terminal 2:
```bash
npm run dev
```

Expected:
- `Express API listening on ...` and `Deployment registered at ...` reappear within ~3 s.
- In the Restate UI, the invocation transitions back to `RUNNING`. The sleep entry continues counting down from where it was (state survived the kill — that's the demo's whole point).

- [ ] **Step 7: Watch the workflow finish**

Once the 60-second sleep elapses, the journal adds:
- `Call(activities/prioritizeIssues)` → completes near-instantly.
- `Call(activities/notifyBridge)` → completes near-instantly (overwhelmingly likely; the rigged health range makes `red_alert` near-certain).

Invocation state transitions to `COMPLETED`.

In Terminal 2 stdout, a WARN-level line appears:
```
🚨 BRIDGE ALERT — USS USS Enterprise NCC-1701-D — RED_ALERT: <subsystem> critical at <N>% health; <N> subsystem(s) below threshold (engineer: Geordi La Forge)
```

(The `USS USS` is correct — the template prefixes `USS ` to the already-`USS`-prefixed ship name. Cross-demo invariant — keep it.)

- [ ] **Step 8: Query the completed status from the Express API**

In Terminal 3:
```bash
curl -fsS http://localhost:3000/status/enterprise-ncc-1701 | jq
```
Expected:
```json
{
  "status": "COMPLETED",
  "output": {
    "shipName": "USS Enterprise NCC-1701-D",
    "diagnosticsDate": "2370-04-08",
    "engineerName": "Geordi La Forge",
    "priority": {
      "severity": "red_alert",
      "summary": "<...>"
    },
    "bridgeNotification": "🚨 BRIDGE ALERT — USS USS Enterprise NCC-1701-D — RED_ALERT: <...> (engineer: Geordi La Forge)"
  }
}
```

If `bridgeNotification` is `null`, the rigging failed (got `green` instead of `red_alert`). The biased health range makes that ~1-in-millions; if it happens twice in a row, recheck `mock.ts`.

- [ ] **Step 9: Repeat Steps 1-8 with a fresh workflow id**

Change the workflow id in Step 4 to `enterprise-ncc-1701-takeoff-2` and re-run. The whole flow should reproduce; the timing tolerance for the kill step should not be brittle.

If the second run fails for any reason, fix the underlying cause — do not paper over with retries. The audience runs this exactly once on stage.

- [ ] **Step 10: Run the full test suite one more time**

```bash
cd demos/05-restate && npm test && npm run check
```
Expected: all tests pass, type-check clean.

- [ ] **Step 11: Verification step — sister-demo file diff**

Open `demos/05-restate/src/diagnostics.ts` and `demos/04-dapr/EnterpriseDiagnostics.ApiService/Workflows/DiagnosticsWorkflow.cs` side-by-side. The fan-out + sleep + conditional notify shape should be recognizably the same algorithm. The audience can flip between them and see the model is the same (workflow-as-code) while the operational footprint and worker model are inverted.

- [ ] **Step 12: Verification step — footprint check**

```bash
cd demos/05-restate && find . -name node_modules -prune -o -type f -print | grep -v "^\./node_modules" | sort
```
Expected: a single-digit number of source files plus README/`.http`/configs. The small file count is itself the dimension-3 (Hosting) callback the talk is set up to make.

---

## Done criteria

- [ ] All 10 tasks above complete.
- [ ] `npm test` passes from a clean checkout (`mock.test.ts` + `notify.test.ts`).
- [ ] `npm run check` passes — strict TypeScript, no `any`, no `@ts-ignore`.
- [ ] `grep -nE "Math\.random|Date\.now|setTimeout|console\.(log|warn|error)" src/diagnostics.ts` prints `clean` — workflow body is deterministic.
- [ ] The Task 10 end-to-end choreography succeeds twice in a row, including the kill-and-resume sequence at T+20 s into the sleep.
- [ ] The Restate UI shows `DiagnosticsWorkflow` (1 handler) + `activities` (7 handlers) registered after `npm run dev` boots, with no manual `restate deployments register` step.
- [ ] The `🚨 BRIDGE ALERT` line appears in WARN-level stdout when severity is `red_alert` or `yellow_alert`, and matches the cross-demo format exactly.
- [ ] The Express `/status/:id` endpoint returns the full `DiagnosticsOutput` JSON with `bridgeNotification` populated on the red/yellow path and `null` on the green path.
- [ ] Source tree under `demos/05-restate/src/` is 6 TypeScript files (5 in `src/` + tests in `src/__tests__/`), preserving the spec §3 structural-delta diagram.

---

## Notes for executors

- **No git commands.** The user runs all git operations manually. Do not run `git add`, `git commit`, `git push`, `git stash`, or any other git command at any point. Each task ends with a verification step instead of a commit. All work happens on `main`.
- **Don't touch `plan.md` or the design spec.** Both are frozen. This plan is the implementation contract. If you find a contradiction with [`docs/superpowers/specs/2026-05-17-restate-typescript-demo-design.md`](../specs/2026-05-17-restate-typescript-demo-design.md), stop and ask before changing either document.
- **Don't add new dependencies.** The 7-dep total in `package.json` is a *talking-point* in the talk — extra deps weaken the small-footprint pitch. If you genuinely think a dep is missing, ask first. (Vitest, jest, axios, dotenv, pino, zod — all explicitly out.)
- **Don't split `services.ts` into one-file-per-handler.** The on-stage payoff is that 7 Dapr `WorkflowActivity` files collapse into one TS file. Preserve it.
- **Don't add OTEL / metrics / structured logging.** Stage demo only.
- **Don't add a Dockerfile for the Node process.** The talk laptop runs `npm run dev` — that's the operational-footprint comparison the talk is built around.
- **Don't introduce a HTTP client lib (axios, node-fetch).** The Node 22 built-in `fetch` is used in `index.ts` and is sufficient.
- **Don't add `dist/` to the repo.** `tsx` runs `.ts` directly; no compile step on stage.
- **If `@restatedev/restate-sdk-clients` v1.x method names differ from what `api.ts` uses** (`workflowSendClient` / `workflowClient` / `workflowOutput`), inspect `node_modules/@restatedev/restate-sdk-clients/dist/*.d.ts` and rename consistently. The Express surface (`POST /start`, `GET /status/:id`) stays the same.
- **If `restate-server` is not available on the talk laptop**, fall back to `docker run -p 8080:8080 -p 9070:9070 restate/restate:1.6.x`. Document the version in the README; never `:latest` on stage.
- **The `USS USS` prefix in the bridge alert** (e.g. `🚨 BRIDGE ALERT — USS USS Enterprise NCC-1701-D`) is intentional — the message template prefixes `USS ` to the already-`USS`-prefixed ship name. This is a cross-demo invariant; do not "fix" it.
