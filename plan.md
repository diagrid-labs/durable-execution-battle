# Durable Execution Battle — Slides + Demos Plan

Authoring plan for the 50-minute conference talk. Source of facts: [`research.md`](./research.md).

## Locked decisions

| Choice | Decision |
|---|---|
| **Talk structure** | Per-dimension comparison (DX → Lang → Hosting → Perf → Community), with a single ~25-min demo block in the middle |
| **Live-demo budget** | ~25 minutes (half the talk) |
| **Demo scenario** | **USS Enterprise diagnostics** — fan-out parallel scans → prioritization (with sleep/wait kill-and-resume window) → conditional bridge alert |
| **Demo languages** | Most idiomatic per framework — Cadence **Go** (forced — Python SDK is archived), Temporal **Java** (Spring Boot), Conductor **Python**, Dapr **.NET** (matches `diagrid-labs/dapr-workflow-versioning` reference), Restate **TypeScript** |
| **Slide tool** | Pure markdown with DemoTime-specific frontmatter |
| **Audience polls** | Two symmetric EngageTime polls (opener: "which have you used?" multi-select; closer: "which would you try first?" single-select) — both started + stopped by DemoTime, with the closing poll's delta narrated into the decision guide |

---

## 1. Talk timeline (50 min)

| Time | Segment | Mode | Duration |
|---|---|---|---|
| 00:00 | **Opening** — title, disclosure (Diagrid), what is durable execution | Slides | 3 min |
| 03:00 | **🗳️ Audience poll (EngageTime)** — "Which durable execution frameworks have you used?" QR code + live results | EngageTime (DemoTime-triggered) | 2 min |
| 05:00 | **Lineage map + 5 dimensions preview** — bridges from the poll into the talk meat | Slides | 1 min |
| 06:00 | **Dim 1 — Developer Experience** — 5-way matrix, quickstart wall-clock comparison, Web UI vs CLI | Slides | 3 min |
| 09:00 | **Dim 2 — Language Support & Features** — SDK matrix, killer features per platform | Slides | 3 min |
| 12:00 | **Dim 3 — Hosting** — self-host complexity, managed cloud, K8s, storage | Slides | 3 min |
| 15:00 | **Dim 4 — Performance & Scalability** — proven scale numbers, architecture diagrams, bottlenecks | Slides | 3 min |
| 18:00 | **DEMO BLOCK — USS Enterprise diagnostics in all 5** | DemoTime | **25 min** |
| 18:00 | ↳ Cadence (Go) — the ancestor; sets up the lineage | Demo | 4 min |
| 22:00 | ↳ Temporal (Java) — workflow-as-code default | Demo | 5 min |
| 27:00 | ↳ Conductor (Python) — workflow-as-data framing | Demo | 5 min |
| 32:00 | ↳ Dapr (.NET) + Diagrid Dashboard | Demo | 6 min |
| 38:00 | ↳ Restate (TS) — single-binary, handler-first | Demo | 5 min |
| 43:00 | **Dim 5 — Community & Support** — license, stars, foundation, commercial backing | Slides | 3 min |
| 46:00 | **🗳️ Closing poll (EngageTime)** — "Which would you try first when you get home?" single-select; Marc reads the delta vs the opening poll live | EngageTime (DemoTime-triggered) | 1.5 min |
| 47:30 | **Decision guide** — "If you want X, pick Y", framed by the poll-delta Marc just saw | Slides | 1.5 min |
| 49:00 | **Wrap + Q&A pointer + repo link** | Slides | 1 min |

Buffer: 5 min built into segment estimates (every demo + dimension is conservatively budgeted).

---

## 2. Repository layout

```
durable-execution-battle/
├── research.md                  # source of facts (already done)
├── plan.md                      # this file
├── README.md                    # what this repo is + how to run demos
│
├── slides/                      # markdown slide deck (DemoTime-compatible)
│   ├── 00-open.md
│   ├── 01-dx.md
│   ├── 02-languages.md
│   ├── 03-hosting.md
│   ├── 04-performance.md
│   ├── 05-demo-intro.md         # title slide for the demo block
│   ├── 06-community.md
│   ├── 07-decision-guide.md
│   └── 08-wrap.md
│
├── demos/
│   ├── shared/
│   │   └── scenario.md          # USS Enterprise diagnostics spec (kept identical across frameworks)
│   │
│   ├── 01-cadence/              # Go
│   │   ├── README.md
│   │   ├── diagnostics.go
│   │   ├── worker/main.go
│   │   ├── starter/main.go
│   │   ├── docker-compose.yml
│   │   └── go.mod
│   │
│   ├── 02-temporal/             # Java 21 + Spring Boot 3 + temporal-spring-boot-starter
│   │   ├── README.md
│   │   ├── pom.xml
│   │   ├── mvnw, mvnw.cmd, .mvn/
│   │   ├── enterprise-diagnostics.http
│   │   └── src/main/
│   │       ├── java/io/diagrid/enterprisediagnostics/
│   │       │   ├── EnterpriseDiagnosticsApplication.java
│   │       │   ├── model/        # records: DiagnosticsInput/Output, ScanInput/Result, Priority, ...
│   │       │   ├── workflow/     # DiagnosticsWorkflow{,Impl}.java
│   │       │   ├── activity/     # 5 Scan*Activity + Prioritize + NotifyBridge + MockAnalysisGenerator
│   │       │   └── api/          # DiagnosticsController.java
│   │       └── resources/application.yml
│   │
│   ├── 03-conductor/            # Python (Conductor code-builder SDK)
│   │   ├── README.md
│   │   ├── diagnostics.py
│   │   ├── workers.py
│   │   ├── starter.py
│   │   ├── docker-compose.yml
│   │   └── requirements.txt
│   │
│   ├── 04-dapr/                 # .NET 10 + Aspire (copied from diagrid-labs/dapr-workflow-versioning)
│   │   ├── README.md
│   │   ├── EnterpriseDiagnostics.AppHost/      # Aspire orchestrator (incl. Diagrid Dashboard + Valkey)
│   │   ├── EnterpriseDiagnostics.ApiService/   # exposes /start, /status; hosts workflow + activities
│   │   ├── EnterpriseDiagnostics.ServiceDefaults/
│   │   └── EnterpriseDiagnostics.sln
│   │
│   └── 05-restate/              # TypeScript
│       ├── README.md
│       ├── src/diagnostics.ts
│       ├── src/services.ts
│       ├── package.json
│       └── tsconfig.json
│
├── .demo/                       # DemoTime scripts (VS Code extension)
│   ├── 00-opening-poll.demo     # opens EngageTime poll, waits, closes it
│   ├── 01-cadence.demo
│   ├── 02-temporal.demo
│   ├── 03-conductor.demo
│   ├── 04-dapr.demo
│   └── 05-restate.demo
│
└── .engage/                     # EngageTime poll definitions
    ├── opening-poll.json        # multi-select familiarity poll
    └── closing-poll.json        # single-select "which would you try first?"
```

The `.demo/` folder also gains `99-closing-poll.demo` (paired with the closing poll).

---

## 3. Slide deck (markdown + DemoTime frontmatter)

Each slide file is a markdown file with a YAML frontmatter block recognized by DemoTime. Slides are pure markdown; code blocks use fenced syntax for highlighting; mermaid diagrams render natively in DemoTime.

### Frontmatter shape

```markdown
---
layout: default       # or 'title', 'section', 'two-columns', 'image'
---

# Slide title

Content...
```

### Per-slide outline

#### `00-open.md` — Opening (3 min before poll, 1 min after, ~7 slides total)

**Before the poll (3 min):**
1. **Title slide** — talk title, speaker, conference, date.
2. **Disclosure** — "I work at Diagrid, the commercial company behind Dapr. I'll be deliberate about flagging Dapr's limitations alongside its strengths."
3. **What is durable execution?** — one-paragraph definition + a Star Trek hook: "Imagine writing `await sleep(30 days)` and it just works — even if the warp core breaches mid-wait."
4. **The 5 contenders** — logos + one-line positioning each. **(This slide stays on screen during the poll so the audience names line up with the answer options.)**

**Poll happens here — see §3.5.**

**After the poll (1 min):**
5. **Lineage map** — Mermaid diagram from research.md §2.
6. **The 5 dimensions we'll evaluate** — preview of upcoming sections.

#### `01-dx.md` — Developer Experience (4 min, ~4 slides)

1. **DX matrix** — table from research.md §8.1.
2. **Quickstart wall-clock** — bar chart of "time to hello-workflow" per framework.
3. **Web UI snapshots** — Temporal Web, Conductor DAG, Restate journal UI side-by-side; Dapr panel labeled "no native OSS UI → Diagrid Dashboard (disclosed)".
4. **Learning curve** — determinism cliff (Temporal/Cadence/Dapr/Restate) vs DSL learning (Conductor).

#### `02-languages.md` — Language Support & Features (3 min, ~4 slides)

1. **SDK matrix** — from research.md §8.2, with maturity flags.
2. **Feature comparison grid** — signals, queries, child workflows, versioning, scheduling, search.
3. **Cadence Python is archived** — flag honestly, recommend Go for any new Cadence work.
4. **Sagas + cron** — note Temporal/Conductor have first-class; Restate ships as documented patterns.

#### `03-hosting.md` — Hosting (3 min, ~3 slides)

1. **Hosting matrix** — from research.md §8.3.
2. **Operational footprint comparison** — visual: Temporal cluster (frontend/history/matching/worker + Cassandra + ES) vs Restate single binary.
3. **Managed cloud landscape** — Temporal Cloud, Restate Cloud, Orkes Cloud, Diagrid Catalyst, Instaclustr Cadence (verify GA status pre-talk).

#### `04-performance.md` — Performance & Scalability (3 min, ~3 slides)

1. **Proven scale numbers** — from research.md §8.4 (caveat: pull fresh Uber/Restate numbers pre-talk).
2. **Architecture diagrams** — Temporal sharded services, Dapr virtual actors, Restate Bifrost log.
3. **Bottlenecks at scale** — Cassandra/ES ops, Dapr state-store IO, Conductor poll overhead, Restate disk IO.

#### `05-demo-intro.md` — Demo block intro (1 min, 2 slides)

1. **The scenario slide** — USS Enterprise diagnostics flow diagram (Mermaid; same diagram reused per framework demo).
2. **The kill-and-resume promise** — "Same workflow, 5 frameworks, same kill point. Watch what survives."

Each per-framework demo block embeds its own intro slide inside the corresponding `.demo` file; see §5.

#### `06-community.md` — Community & Support (3 min, ~2 slides)

1. **Community matrix** — from research.md §8.5 (license, stars, foundation, commercial).
2. **Single-vendor risk callout** — Temporal Inc., Orkes, Diagrid, Restate GmbH are single-vendor stewards; Dapr (CNCF graduated) and Cadence (Uber, Apache) have broader governance.

#### `07-decision-guide.md` — Decision guide (1.5 min, 1 slide)

Single dense slide with the "if you want X, pick Y" bullets from research.md §9. **Closing poll happens immediately before this slide** — see §3.5. Marc opens this slide by narrating the poll delta vs the opener ("Restate went from 5% to 28% — that's earned. Here's where each fits…").

#### `08-wrap.md` — Wrap-up (1 min, 2 slides)

1. **Code repo + slides QR code** — GitHub URL.
2. **Q&A + thanks** — contact info.

---

## 3.5. Audience polls (EngageTime) — opener + closer

Two symmetric polls bookend the talk. The opener calibrates Marc's framing during the dimension walkthrough; the closer measures whether the talk moved minds and feeds the decision-guide narration.

### Poll A — Opener: "Which have you used?"

Goal: in 2 minutes, get a live signal of how familiar the room is with each framework — Marc uses the result to calibrate framing for the rest of the talk.

#### Placement
Between slide 4 ("The 5 contenders") and slide 5 ("Lineage map") in `00-open.md`. The contenders slide stays visible during the poll so the names match the answer options.

### Poll definition (`.engage/opening-poll.json`)

A single **multi-select** question — fastest to answer, cleanest results.

```jsonc
{
  "id": "opening-familiarity",
  "title": "Which durable execution frameworks have you used?",
  "subtitle": "Production, prototype, or serious evaluation — pick all that apply",
  "type": "multi-select",
  "options": [
    { "id": "temporal",  "label": "Temporal" },
    { "id": "cadence",   "label": "Cadence" },
    { "id": "conductor", "label": "Conductor" },
    { "id": "dapr",      "label": "Dapr Workflows" },
    { "id": "restate",   "label": "Restate" },
    { "id": "none",      "label": "None — new to durable execution" }
  ],
  "display": {
    "qr": true,
    "showResultsLive": true,
    "resultStyle": "bar-chart-horizontal",
    "anonymize": true
  },
  "duration": { "seconds": 90 }
}
```

### Lifecycle (DemoTime → EngageTime)

DemoTime drives the poll's open/close so Marc doesn't fumble with two extensions on stage. A single `.demo` step opens the EngageTime panel (which renders the QR code + question on the secondary monitor), and another step stops it when results have stabilized. See §6 for the `.demo` snippet.

### Choreography (2 min)

| t | What happens | Marc says |
|---|---|---|
| 0:00 | DemoTime triggers `engageTime.start("opening-familiarity")`. QR code + question appear on the projector. | "Before I tell you what I think of these five, I want to hear from you. Pull out your phones and scan." |
| 0:15 | Audience scans, answers start streaming in; bar chart fills live. | (silent while audience answers) |
| 1:00 | Bar chart roughly stable. | (Read the room out loud) "OK — looks like ~half of you have used Temporal, ~a third Dapr, ~one brave soul has tried Restate. That tells me where to spend more time." |
| 1:45 | DemoTime triggers `engageTime.stop()` + advances to lineage slide. | "Let's see how they got here." |

### Reading the room — pre-baked reactions

| Pattern | Marc's calibration |
|---|---|
| Heavy Temporal majority | "Most of you know the workflow-as-code model. I'll go slightly faster on Temporal/Cadence and spend more time on Restate's distinct programming model and Conductor's workflow-as-data framing." |
| Heavy Dapr majority | "Lots of Dapr users — quick disclosure reminder, my employer makes the commercial Dapr offering. I'll lean harder into where Temporal and Restate beat Dapr today so the comparison stays honest." |
| Even spread | "Good — you'll have a Goldilocks talk; every framework gets equal demo time." |
| Mostly "None — new to durable execution" | "Welcome to the category. I'll define 'activity', 'workflow history', and 'replay' as we go — flag me if I jargon-bomb you." |

### Poll A fallback
If EngageTime fails on stage (extension crash, projector mirror issue, no audience phones), skip silently — DemoTime advances past the poll step with a single keystroke. Have a **static slide** as the fallback that just lists the 5 framework names + "How many of you have used these? Show of hands." Marc reads the room visually instead of via live results.

---

### Poll B — Closer: "Which would you try first?"

Goal: in 1.5 minutes at 46:00, measure whether the talk moved minds, and feed the delta into the decision guide.

#### Placement
Immediately after Dim 5 (Community & Support) and immediately before the Decision guide slide. The poll *is* the segue.

#### Poll definition (`.engage/closing-poll.json`)

**Single-select** this time — audience commits to one pick, which sharpens the comparison and produces a cleaner bar chart.

```jsonc
{
  "id": "closing-pick",
  "title": "Which would you try first when you get home?",
  "subtitle": "Just one — based on what you saw today",
  "type": "single-select",
  "options": [
    { "id": "temporal",  "label": "Temporal" },
    { "id": "cadence",   "label": "Cadence" },
    { "id": "conductor", "label": "Conductor" },
    { "id": "dapr",      "label": "Dapr Workflows" },
    { "id": "restate",   "label": "Restate" },
    { "id": "exploring", "label": "Still exploring / depends" }
  ],
  "display": {
    "qr": true,
    "showResultsLive": true,
    "resultStyle": "bar-chart-horizontal",
    "compareTo": "opening-familiarity",
    "anonymize": true
  },
  "duration": { "seconds": 75 }
}
```

The `compareTo` field tells EngageTime to render the bar chart with **opening-poll percentages overlaid as a faint ghost bar** so the delta is visible at a glance. (Verify the field name against the EngageTime extension before authoring — the concept stands, the literal key may differ.)

#### Choreography (1.5 min)

| t | What happens | Marc says |
|---|---|---|
| 0:00 | DemoTime triggers `engageTime.startPoll("closing-pick")`. QR + question appear. | "One last vote. Based on everything you just saw — which one would you actually try first?" |
| 0:10 | Answers stream in; bar chart fills against the ghost bars of the opening poll. | (silent while audience answers) |
| 0:50 | Bar chart stable. | (Read the deltas) "Look at that — Restate jumped from 5% to 28%. Dapr held steady. That's the talk doing its job. Now here's where each one actually fits…" |
| 1:30 | DemoTime stops the poll + advances to decision-guide slide. | (transitions into decision guide) |

#### Reading the delta — pre-baked reactions

| Pattern | Marc's transition into the decision guide |
|---|---|
| Restate surges | "Restate is the surprise of the talk — small footprint, novel model. If you build something new today, it deserves a spot on the shortlist." |
| Dapr drops | (acknowledge honestly) "Dapr lost some ground — fair. The versioning story is brand-new and the OSS UI gap is real. The strengths are still the strengths, but Temporal owns 'mature default.'" |
| Temporal stays dominant | "Temporal is still where most of you are going — that tracks the maturity gap." |
| Big shift to "Still exploring" | "Good — that's the honest answer. Slide's coming up to help you pick once you know your workload." |

#### Poll B fallback
Same as Poll A — DemoTime skips with a keystroke; Marc goes straight to the decision guide without the poll-delta cold-open. If only the *compareTo* feature fails (poll runs but no overlay), narrate the deltas from memory or from a quick visual check against the opening poll still on the projector via a window-swap shortcut.

---

## 4. The shared demo scenario — USS Enterprise diagnostics

### Specification (lives at `demos/shared/scenario.md`)

```
DiagnosticsWorkflow(shipId, missionId):
  # ─── Fan-out: 5 parallel subsystem scans ───
  scans = parallel [
    activity(ScanWarpCore,    { shipName, subsystem: "warp_core" }),
    activity(ScanShields,     { shipName, subsystem: "shields" }),
    activity(ScanLifeSupport, { shipName, subsystem: "life_support" }),
    activity(ScanSensors,     { shipName, subsystem: "sensors" }),
    activity(ScanWeapons,     { shipName, subsystem: "weapons" })
  ]
  results = await all(scans)             # ── Fan-in ──

  # ─── Prioritization with kill/resume window ───
  log("Engineering review pending — sleeping 60s")
  await sleep(60 seconds)                # ⚡ KILL POINT ⚡
  priority = activity(PrioritizeIssues, { shipName, scans: results })

  # ─── Conditional bridge notification ───
  if priority.severity in {"yellow_alert", "red_alert"}:
    activity(NotifyBridge, { shipName, engineerName, priority })

  return { shipName, diagnosticsDate, engineerName, priority, bridgeNotification }
```

### Activity behavior (canonical reference: `demos/04-dapr/` .NET)

| Activity | Input | Output | How to simulate |
|---|---|---|---|
| `ScanWarpCore` | `{ shipName, subsystem: "warp_core" }` | `{ subsystem, health: 30-65, anomalies: [...] }` | No artificial latency — return immediately. Random health biased low + 1-3 anomalies sampled from a per-subsystem candidate list |
| `ScanShields` | `{ shipName, subsystem: "shields" }` | Same shape | Same |
| `ScanLifeSupport` | `{ shipName, subsystem: "life_support" }` | Same shape | Same |
| `ScanSensors` | `{ shipName, subsystem: "sensors" }` | Same shape | Same |
| `ScanWeapons` | `{ shipName, subsystem: "weapons" }` | Same shape | Same |
| `PrioritizeIssues` | `{ shipName, scans: ScanResult[] }` | `{ severity, summary }` where `severity ∈ { "green", "yellow_alert", "red_alert" }` | Deterministic ranking on `min(health)`: `< 50 → red_alert`, `< 75 → yellow_alert`, else `green`. `summary` names the worst subsystem + count below threshold |
| `NotifyBridge` | `{ shipName, engineerName, priority }` | `{ message }` | Logs a styled `🚨 BRIDGE ALERT — USS {ship} — {SEVERITY}: {summary} (engineer: {name})` line via the worker's stdout. No external service — the alert lands inside the framework UI (event history / workflow journal) |

**Demo drama — rig via biased scan range, not severity override.** Each scan returns `health` from a low-biased range (30–65) so `min(health) < 50` is almost certain → severity reliably lands on `red_alert` → conditional `NotifyBridge` fires every run → audience sees the activity appear in the framework's workflow UI. This keeps `PrioritizeIssues` honest (deterministic on real inputs) instead of hardcoding the outcome.

**No artificial scan latency.** The earlier draft prescribed `sleep(rand 2-4s)` per scan to "give time to pause the workflow." That's no longer needed — the kill point is the 60-second `sleep` *between* fan-in and prioritization, which provides all the window required. Scans return as fast as the framework can dispatch them, which makes the fan-out visually snappy on stage.

### The kill-and-resume choreography (identical per framework)

1. Start the workflow (~1 sec). Workflow ID = `enterprise-ncc-1701`.
2. Audience sees 5 parallel scans complete in the UI (~5 sec).
3. Workflow enters the 60-second sleep. **A countdown timer overlays the screen** (DemoTime feature) to build tension.
4. At ~T+20s into the sleep, Marc kills the worker process (Ctrl+C or `kill -9`).
5. Marc says: "If this were durable, the workflow would survive. Let's see."
6. Marc restarts the worker.
7. The workflow's pending sleep durably wakes; the prioritization activity runs; the bridge alert fires.
8. The framework UI (Temporal Web event history, Conductor DAG, Diagrid Dashboard, Restate journal, Cadence Web) shows the `NotifyBridge` activity completing; DemoTime overlays a 🚨 graphic.
9. Cut to next framework.

### No external front-end for the bridge alert

The bridge notification is **intentionally internal** — `NotifyBridge` is a normal workflow activity that logs to the worker's stdout. There is no separate HTTP server, no UI to navigate to. The visible payoff for each framework is its **own** workflow UI showing the activity completing inside the durable history. That keeps each demo apples-to-apples and avoids one extra moving part on stage.

---

## 5. Per-framework demo apps

Each demo follows the identical scenario, just implemented in the framework's idiomatic way. Each has its own `README.md` with step-by-step run instructions.

### 5.0 Idiomatic naming — do NOT transliterate the .NET reference

The Dapr/.NET demo at `demos/04-dapr/` is the **canonical scenario reference** (activity behavior, severity thresholds, rigging strategy, kill-and-resume choreography). It is **not** a style guide for the other four implementations.

Each demo MUST use the host language's and framework's native conventions for:

- **Filenames and folder layout** — e.g. `diagnostics.go` / `snake_case.py` / `kebab-case.ts` / `PascalCase.java`; Go's flat package, Python's `__init__.py`, Maven's `src/main/java/...`, TypeScript's `src/`.
- **Class / type / record / struct names** — e.g. Go uses unexported `scanResult` if package-private, Python uses `dataclass`/`TypedDict`, TS uses `interface`/`type`, Java uses `record`. Don't force `PascalCase` everywhere.
- **Method / function names** — `camelCase` in Java/TS, `snake_case` in Python, `PascalCase` exported in Go.
- **Variable names** — short, idiomatic locals (`ctx`, `err` in Go; `_` for unused in Python; etc.).
- **Async/concurrency primitives** — `Promise.allOf` in Java, `asyncio.gather` in Python, `Promise.all` in TS, `workflow.Future` + `selector` in Go. Match the SDK's natural surface, not the .NET `Task.WhenAll` shape.
- **Error handling** — Go's `if err != nil`, Python exceptions, TS try/catch, Java checked vs unchecked — use what the SDK and language expect.
- **Logging / formatting / config** — language-native (slog/zap in Go, logging in Python, console/pino in TS, SLF4J in Java; `.env` / YAML / TOML as the ecosystem prefers).

What MUST stay constant across all five demos (so on-stage comparisons read cleanly):

- **The five scan subsystem identifiers** as wire/string values: `warp_core`, `shields`, `life_support`, `sensors`, `weapons`.
- **Severity wire values**: `green`, `yellow_alert`, `red_alert` (snake_case in JSON regardless of in-language enum casing).
- **Severity thresholds**: `min(health) < 50 → red_alert`, `< 75 → yellow_alert`, else `green`.
- **Health rigging range**: `30..65` inclusive (biased low so `red_alert` is overwhelmingly likely).
- **Bridge alert message format**: `🚨 BRIDGE ALERT — USS {shipName} — {SEVERITY}: {summary} (engineer: {engineerName})`.
- **Workflow id used on stage**: `enterprise-ncc-1701`.
- **Kill point**: a 60-second durable sleep/timer between fan-in and prioritization.
- **Conditional notify**: bridge notification fires only on `yellow_alert` or `red_alert`; the output's bridge-notification field is nullable/optional so the green path stays observable.

Translation lens: if you find yourself writing `DiagnosticsWorkflowImpl` in Go or `Task.WhenAll` in Python, stop — that's transliteration. Read how Temporal/Cadence/Conductor/Restate sample code is written in *that* language's docs, and match that voice.

### 5.1 Cadence — Go (4 min)

- **File**: `demos/01-cadence/diagnostics.go`
- **Workflow shape**: top-level `func DiagnosticsWorkflow(ctx workflow.Context, ...) (Priority, error)`.
- **Fan-out**: 5 `workflow.ExecuteActivity` calls, store futures in a slice, `selector.Select(ctx)` or loop `.Get(ctx, &result)` over each.
- **Sleep**: `workflow.Sleep(ctx, 60*time.Second)`.
- **Runtime**: docker-compose with Cassandra + Cadence server (pre-pull all images — see §7); Cadence Web at `localhost:8088`.
- **Talking point on stage**: "Cadence is the ancestor. Temporal is literally a fork by Cadence's original authors. Same model, divergent stewardship. The next demo (Temporal in Java) shows where the model went after the fork."

### 5.2 Temporal — Java (5 min)

- **Module**: `demos/02-temporal/` — Java 21 + Spring Boot 3 + `temporal-spring-boot-starter` (the `-alpha` line was discontinued at 1.23.2; the GA starter at 1.30.x ships dual Spring-Boot-2/3 auto-config). See [`docs/superpowers/specs/2026-05-16-temporal-java-demo-design.md`](docs/superpowers/specs/2026-05-16-temporal-java-demo-design.md) for the full design.
- **Workflow shape**: `@WorkflowInterface` + `@WorkflowMethod` on `DiagnosticsWorkflow`; `@WorkflowImpl(taskQueues = "diagnostics-tq")` on `DiagnosticsWorkflowImpl`. Activities are `@ActivityInterface` + `@Component @ActivityImpl(...)` impls auto-discovered by the Spring Boot starter.
- **Fan-out**: 5 `Promise<ScanResult> = Async.function(stub::execute, new ScanInput(shipName, subsystem))` calls, then `Promise.allOf(...).get()` to fan in.
- **Sleep**: `Workflow.sleep(Duration.ofSeconds(60))` — the durable kill point.
- **Conditional notify**: standard `if (severity == YELLOW_ALERT || severity == RED_ALERT)` branch; `DiagnosticsOutput.bridgeNotification` is **nullable** so the green path is observable.
- **Runtime**: `temporal server start-dev` in one terminal (Temporal CLI dev server; no Docker), `./mvnw spring-boot:run` in another (worker + REST API in one process). Kick off via `POST /start` from `enterprise-diagnostics.http`.
- **Visual**: Temporal Web at `localhost:8233` — show event history with the 5 `ActivityTaskCompleted` events for the parallel scans, then `TimerStarted` (60s) → Marc kills the Spring Boot app → restart → `TimerFired` → `PrioritizeIssuesActivity` → `NotifyBridgeActivity` → `WorkflowExecutionCompleted`.
- **Idiomatic tip to call out**: "Workflow code is a sandboxed deterministic class — no `Random`, no `LocalDateTime.now()`, no `Thread.sleep`. Activities are normal Spring beans where the side-effects live. The Spring Boot starter auto-wires the worker — same DI feel as the .NET Dapr demo coming up." This bridges into the Dapr/.NET comparison two demos later.

### 5.3 Conductor — Python with code-builder SDK (5 min)

- **File**: `demos/03-conductor/diagnostics.py`
- **Workflow shape**: `ConductorWorkflow` built via `>>` chaining + `FORK_JOIN` task type for the parallel scans.
- **Fan-out**: `FORK_JOIN` system task — declarative, the engine runs it; workers don't see the fork.
- **Sleep**: `WAIT` system task with 60s duration (or `INLINE` task with `setTimeout`).
- **Conditional notify**: `SWITCH` task with a JQ expression on `${prioritize.output.severity}`.
- **Runtime**: `docker run -p 8080:8080 -p 5000:5000 conductoross/conductor-standalone:latest`; UI at `localhost:5000`.
- **Talking point on stage**: "This is the only framework where the workflow is *data*, not code. The Python builder is sugar; the actual workflow is JSON the engine interprets. Strengths and trade-offs flow from that."
- **Visual moment**: open the **Workflow Definitions** panel and show the generated JSON — proof that "workflow-as-code" here is really JSON-builder ergonomics.

### 5.4 Dapr — .NET (6 min, with Aspire + Diagrid Dashboard) — **canonical reference**

This demo is the **canonical implementation** of the shared scenario; the other four framework demos should mirror its activity names, model shapes, severity thresholds, and rigging strategy.

- **Project**: `demos/04-dapr/` — .NET 10 + Aspire orchestrating ApiService + Valkey state store + Diagrid Dashboard container. Seeded from `diagrid-labs/dapr-workflow-versioning/EnterpriseDiagnostics` and already adapted to the shared scenario (5 `Scan*` activities, 60s `CreateTimer`, conditional `NotifyBridge`).
- **Workflow shape**: `internal sealed partial class DiagnosticsWorkflow : Workflow<DiagnosticsInput, DiagnosticsOutput>`; activities are `internal sealed partial class Scan...Activity : WorkflowActivity<ScanInput, ScanResult>`.
- **Activities** (in `EnterpriseDiagnostics.ApiService/Activities/`):
  - `ScanWarpCoreActivity`, `ScanShieldsActivity`, `ScanLifeSupportActivity`, `ScanSensorsActivity`, `ScanWeaponsActivity` — each returns `ScanResult(Subsystem, Health, Anomalies[])` via `MockAnalysisGenerator` (health 30-65, 1-3 anomalies sampled from a per-subsystem candidate list). **No `Thread.Sleep`** — scans return immediately.
  - `PrioritizeIssuesActivity` — input `PrioritizeInput(ShipName, Scans[])`, output `Priority(Severity, Summary)`. Severity from `min(health)`: `< 50 → "red_alert"`, `< 75 → "yellow_alert"`, else `"green"`.
  - `NotifyBridgeActivity` — input `BridgeNotificationInput(ShipName, EngineerName, Priority)`, output `BridgeNotificationResult(Message)`. Emits `🚨 BRIDGE ALERT — USS {ShipName} — {SEVERITY}: {Summary} (engineer: {EngineerName})`.
- **Fan-out**: `Task.WhenAll(...)` over 5 `context.CallActivityAsync<ScanResult>(nameof(Scan...Activity), new ScanInput(shipName, subsystem), retryPolicy)` calls.
- **Sleep**: `await context.CreateTimer(TimeSpan.FromSeconds(60));`.
- **Conditional notify**: `if (priority.Severity is "yellow_alert" or "red_alert") { await context.CallActivityAsync(nameof(NotifyBridgeActivity), ...); }`. `DiagnosticsOutput.BridgeNotification` is **nullable** so the green path is observable (null = "notification skipped, severity was green").
- **Rigging**: `MockAnalysisGenerator.Generate` returns `health = Random.Next(30, 66)` — biased low so `min(health) < 50` is overwhelmingly likely → severity reliably hits `red_alert` → bridge alert fires every run.
- **Runtime**: `aspire run` from `demos/04-dapr/`. Aspire boots Valkey, the ApiService + Dapr sidecar, and the Diagrid Dashboard container in one shot.
- **Demo arc** (6 min):
  1. Show `Workflows/DiagnosticsWorkflow.cs` + the 5 `Scan*` activities. Highlight the `Task.WhenAll` fan-out + the `CreateTimer(60s)` sleep + the conditional `NotifyBridge` branch.
  2. Start a diagnostics instance via the `.http` file (`POST /start`, instance id `enterprise-ncc-1701`).
  3. Aspire dashboard: ApiService, sidecar, Valkey, Diagrid Dashboard all healthy. Open the **Diagrid Dashboard** — 5 parallel scan activities complete near-instantly.
  4. Workflow enters the 60s timer. Kill the ApiService process (or the Dapr sidecar) mid-sleep. The Diagrid Dashboard shows the instance frozen on the timer.
  5. Restart the ApiService — the timer durably wakes, `PrioritizeIssues` runs, `NotifyBridge` fires with the 🚨 banner (visible in the Diagrid Dashboard's activity timeline + the worker stdout).
- **Diagrid disclosure phrasing**: *"To visualize Dapr workflows I'm using the free Diagrid Dashboard — full disclosure, Diagrid is my employer. It's an anonymously-pulled container; Dapr OSS doesn't ship a Web UI, so this fills the gap locally."*

### 5.5 Restate — TypeScript (5 min)

- **File**: `demos/05-restate/src/diagnostics.ts`
- **Workflow shape**: `restate.workflow("DiagnosticsWorkflow", { run: async (ctx, input) => { ... } })`. Activities are sibling services (or `ctx.run` blocks for inline).
- **Fan-out**: `await Promise.all([ctx.run("scan-warpCore", ...), ...])`.
- **Sleep**: `await ctx.sleep({ seconds: 60 })`.
- **Conditional notify**: standard `if`; the notify is an inline `ctx.run("notify-bridge", () => { /* log a styled banner */ })` so the alert lands in the durable journal without any external service.
- **Runtime**: one binary — `restate-server` (or pinned Docker image) + `npm run dev`; admin UI at `localhost:9070`.
- **Killer talking point on stage**: *"There's no worker polling a task queue. Restate is calling my HTTP handler. The journal lives in the server, not the SDK. One binary, no Postgres, no Kafka, no Cassandra."*
- **Killer demo beat**: `kill -9` the Node process during the sleep. Restate auto-redrives the handler the moment it's back. Admin UI shows journal entries lighting up green.

---

## 6. DemoTime `.demo` files

DemoTime is a VS Code extension; `.demo` files are JSON specifying ordered steps (open file, type text, run terminal command, show overlay, etc.). One `.demo` file per framework demo, plus one for the opening poll.

### `00-opening-poll.demo` — drives the EngageTime poll

DemoTime starts and stops the poll so Marc only operates one extension on stage. EngageTime exposes commands (e.g. `engageTime.startPoll`, `engageTime.stopPoll`) that DemoTime invokes via the `executeCommand` action.

```jsonc
{
  "title": "Opening — audience familiarity poll",
  "description": "Multi-select poll started/stopped by DemoTime",
  "demos": [
    { "title": "Start the poll (shows QR + bar chart on projector)", "steps": [
      { "action": "executeCommand", "command": "engageTime.startPoll",
        "args": { "pollId": "opening-familiarity" } }
    ]},
    { "title": "Wait for audience answers (~90s, advance manually)", "steps": [
      { "action": "waitForInput", "message": "Audience answering — advance when results stabilize" }
    ]},
    { "title": "Stop the poll (results stay frozen on screen)", "steps": [
      { "action": "executeCommand", "command": "engageTime.stopPoll" }
    ]},
    { "title": "Advance to lineage slide", "steps": [
      { "action": "openSlide", "path": "slides/00-open.md", "slideIndex": 5 }
    ]}
  ]
}
```

> **Verify pre-talk**: confirm the exact EngageTime command IDs (`engageTime.startPoll` / `engageTime.stopPoll` / their argument shapes) against the EngageTime VS Code extension's `package.json` — they may be `engagetime.start` or similar. The pattern stands; only the literal command names need confirming.

### `99-closing-poll.demo` — drives the closing poll

Same shape as the opener, just bound to the `closing-pick` poll. Sits right before the decision-guide slide:

```jsonc
{
  "title": "Closing — 'which would you try first?'",
  "description": "Single-select poll with delta-vs-opener overlay; bridges into the decision guide",
  "demos": [
    { "title": "Start the closing poll", "steps": [
      { "action": "executeCommand", "command": "engageTime.startPoll",
        "args": { "pollId": "closing-pick" } }
    ]},
    { "title": "Wait for audience answers (~75s)", "steps": [
      { "action": "waitForInput", "message": "Audience answering — advance when bars are stable" }
    ]},
    { "title": "Stop the poll (results stay frozen for the decision-guide narration)", "steps": [
      { "action": "executeCommand", "command": "engageTime.stopPoll" }
    ]},
    { "title": "Advance to decision guide", "steps": [
      { "action": "openSlide", "path": "slides/07-decision-guide.md", "slideIndex": 0 }
    ]}
  ]
}
```

### Common pattern per framework `.demo` file

```jsonc
{
  "title": "Temporal — USS Enterprise diagnostics (Java)",
  "description": "Fan-out scans, durable sleep, kill+resume, conditional bridge alert",
  "demos": [
    { "title": "Show the workflow code", "steps": [
      { "action": "open", "path": "demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflowImpl.java" },
      { "action": "highlight", "path": "demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflowImpl.java", "startLine": 20, "endLine": 55 }
    ]},
    { "title": "Start the server", "steps": [
      { "action": "executeTerminalCommand", "command": "temporal server start-dev", "terminalId": "server" }
    ]},
    { "title": "Start the Spring Boot app (worker + REST API)", "steps": [
      { "action": "executeTerminalCommand", "command": "cd demos/02-temporal && ./mvnw spring-boot:run", "terminalId": "worker" }
    ]},
    { "title": "Kick off a workflow via POST /start", "steps": [
      { "action": "open", "path": "demos/02-temporal/enterprise-diagnostics.http" }
    ]},
    { "title": "Open Temporal Web", "steps": [
      { "action": "openExternal", "url": "http://localhost:8233" }
    ]},
    { "title": "Kill point — Ctrl+C the Spring Boot app during the 60s timer", "steps": [
      { "action": "sendKey", "terminalId": "worker", "key": "Ctrl+C" },
      { "action": "wait", "ms": 3000 },
      { "action": "executeTerminalCommand", "command": "cd demos/02-temporal && ./mvnw spring-boot:run", "terminalId": "worker" }
    ]},
    { "title": "Bridge alert lands in worker stdout", "steps": [
      { "action": "focusTerminal", "terminalId": "worker" }
    ]}
  ]
}
```

Author one `.demo` per framework. Use `terminalId` consistently so terminals don't multiply on stage.

### DemoTime tips for this talk

- **Use the countdown overlay** during the 60-second sleep — it builds tension and tells the audience exactly when the kill happens.
- **Use named terminals** (`worker`, `starter`, `bridge`, `server`) so the on-screen pane labels are readable.
- **Pre-stage the editor** to the workflow file before each demo — no fumbling for files on stage.
- **Record fallback videos** per demo and reference them in DemoTime's `fallback` field (DemoTime supports auto-fallback to a video clip if a step fails).

---

## 7. Pre-talk checklist

Author this as a literal checklist in `README.md`:

### Day-before
- [ ] `docker pull` every image used by every demo:
  - `temporalio/server:latest` (pin a version)
  - `ubercadence/server:latest` (pin)
  - `cassandra:4` (Cadence dep)
  - `conductoross/conductor-standalone:v3.30.0.rc15`
  - `ghcr.io/diagridio/diagrid-dashboard:0.0.3`
  - `daprio/dapr:1.17.7`
  - `valkey/valkey:latest` (Dapr state store)
  - `restate/restate:1.6.2` (or whatever's current; pin)
- [ ] `brew install temporal` (Temporal CLI dev server is a single binary — no Docker needed for the Temporal/Java demo).
- [ ] In `demos/02-temporal/`, run `./mvnw dependency:go-offline` to warm the local Maven cache so the on-stage Spring Boot startup doesn't pull from Maven Central.
- [ ] Run each demo end-to-end on the talk laptop (not just the dev machine).
- [ ] **Run both polls end-to-end with a second device** (phone) acting as the audience — confirm QR scan works, results render live, DemoTime start/stop commands fire for both `opening-familiarity` and `closing-pick`, and the closing poll's `compareTo` overlay actually renders against the opener's results. Confirm fallback path works for each.
- [ ] Record a 30-60s fallback video for each of the 5 demos.
- [ ] Verify Diagrid Dashboard surfaces V1 vs V2 distinctly — if not, prepare a verbal explanation slide.
- [ ] Pin all framework versions in compose files / project files; never use `:latest` on stage.
- [ ] Verify all sound effects + DemoTime overlays render on the projector (different colorspace from laptop screen).

### Verify-before-stage items (from `research.md` §11)
- [ ] Temporal Cloud pricing — refresh from `temporal.io/pricing`.
- [ ] Uber Cadence scale numbers — most recent blog/talk.
- [ ] Instaclustr Managed Cadence — still GA?
- [ ] Orkes Cloud regions and free-tier limits.
- [ ] Restate benchmark numbers — most recent blog post.
- [ ] All 5 star counts — refresh morning of talk.

### On-stage
- [ ] Pre-warm: do a single `docker run` of each long-pull image (Cassandra) right before the talk to bring them out of cold cache.
- [ ] Disable Wi-Fi-dependent integrations (LangSmith tracing in Temporal demos, etc.) so a flaky conference network doesn't kill the demo.
- [ ] Have the fallback videos one keypress away in OBS / Keynote.

---

## 8. Authoring order

Suggested build sequence — fastest payoff first, riskiest last:

1. **Dapr demo (canonical reference)** — already done in `demos/04-dapr/`. .NET 10 + Aspire, 5 `Scan*` activities, 60s `CreateTimer`, conditional `NotifyBridge`. Use this as the spec when implementing the other four framework demos: match the activity names, the `ScanResult { subsystem, health, anomalies }` and `Priority { severity, summary }` shapes, the severity thresholds (`<50 → red_alert`, `<75 → yellow_alert`), and the low-biased health range for demo rigging.
2. **Temporal demo (Java)** — Spring Boot + `temporal-spring-boot-starter` (the `-alpha` line was discontinued at 1.23.2; GA ships at 1.30.x); sanity-check the scenario shape end-to-end. See [`docs/superpowers/specs/2026-05-16-temporal-java-demo-design.md`](docs/superpowers/specs/2026-05-16-temporal-java-demo-design.md). ~4 hours.
3. **Restate demo** — single binary, no infra; quick second win. ~2 hours.
4. **Conductor demo** — JSON DSL is a different mental model; budget time for the FORK_JOIN + SWITCH wiring. ~3 hours.
5. **Cadence demo** — last because it's almost identical to Temporal in Go; cheap copy/translate. ~2 hours.
6. **Slide deck** — write last, once the demos are real and you know what visuals you actually have. ~6 hours.
7. **DemoTime scripts** — last; choreographed against the working demos. ~3 hours.
8. **EngageTime polls + DemoTime triggers** — author `.engage/opening-poll.json`, `.engage/closing-poll.json`, `00-opening-poll.demo`, `99-closing-poll.demo`; test the start/stop commands and the closing-poll `compareTo` overlay locally with a second device acting as audience. ~1.5 hours.

Total: ~24-29 hours of focused authoring, plus ~5 hours of dry runs.

---

## 9. Risks + mitigations

| Risk | Mitigation |
|---|---|
| 5 demos × ~5 min = no buffer for failures | Cut the Cadence demo to 2 min (it's nearly identical to Temporal — show the code diff, skip the runtime) if running long |
| Conference Wi-Fi blocks `docker pull` | Pre-pull everything day-before; never pull on stage |
| Temporal/Cadence Cassandra container takes >2 min to start cold | Start docker-compose during the preceding dimension slides, not at demo time |
| Audience asks: "Why is the Dapr demo .NET when the rest are Python/TS?" | Pre-bake the answer: ".NET shows the source-generator versioning pattern, which is the most distinctive new thing in Dapr 1.17. Python/Go versioning APIs are also covered on the language slide." |
| "Why didn't you include Azure Durable Functions / AWS Step Functions / etc.?" | Have a backup slide: scope was OSS workflow-as-code platforms with self-hostable runtimes — proprietary cloud-only services excluded by design |
| EngageTime poll fails on stage (extension crash, projector mirror issue, no phones in the room) | DemoTime skips the poll step with one keystroke; fallback slide shows the 5 names + "show of hands"; Marc reads the room visually. Pre-talk checklist includes a poll dry-run with a second device. |
| Poll dominates time / audience won't stop typing answers | Hard-capped at 90s (opener) / 75s (closer) by the `duration` field; DemoTime stops the poll regardless. |
| Closing-poll `compareTo` overlay doesn't render against the opener | Marc narrates the deltas from memory by glancing at the opener's results on a second window; the bar chart is still useful even without the ghost overlay. Confirm `compareTo` field name pre-stage. |
| Audience size shrinks between opening and closing poll (people leave) | Compare percentages, not absolute counts. Note it on stage if dramatic ("we lost some folks — comparing rates not totals"). |

---

## 10. What's NOT in scope

- Production deployment guides per framework.
- Long-running cost or scale comparisons.
- Migration playbooks between frameworks.
- Anything about closed-source competitors (AWS SWF/SFN, Azure Durable Functions, GCP Workflows).

These deserve their own talks.
