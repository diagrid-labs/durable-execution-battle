is the super# Durable Execution Battle — Research

> Research brief for the 50-minute conference talk **"Durable execution battle: comparing open-source workflow-as-code platforms."** Compares Temporal, Conductor, Dapr Workflows, Restate, and Cadence across developer experience, language support & features, hosting options, performance & scalability, and community & support. Includes per-platform demo plans for use with DemoTime.

**Brief date:** May 16, 2026
**Speaker:** Marc Duiker (Diagrid)

> ⚠️ **Sourcing note.** First-pass briefs were written from training data through early 2026. **Round-2 verification (2026-05-16)** confirmed everything via GitHub WebFetch — licenses, latest versions, star counts, Dapr versioning details, Restate operator/HA, .NET + Java versioning APIs. Items that remain "Marc must verify manually" (pricing pages, Uber blog scale numbers, etc.) are restricted to non-GitHub commercial domains that the sandbox blocked, and are explicitly flagged in §11.

> ⚠️ **Speaker disclosure.** Marc is employed by Diagrid, the commercial company behind Dapr/Catalyst. The Dapr section is written with deliberate honesty about limitations vs the alternatives; disclosure should be made explicitly on the opening slide and again whenever showing Diagrid Catalyst / Conductor as a "managed UI" visual.

---

## Table of Contents

1. [TL;DR — quick comparison matrix](#1-tldr--quick-comparison-matrix)
2. [Framing the category](#2-framing-the-category)
3. [Temporal](#3-temporal)
4. [Cadence](#4-cadence)
5. [Conductor](#5-conductor)
6. [Dapr Workflows](#6-dapr-workflows)
7. [Restate](#7-restate)
8. [Cross-cutting comparison per dimension](#8-cross-cutting-comparison-per-dimension)
9. [Recommended 50-minute talk structure](#9-recommended-50-minute-talk-structure)
10. [DemoTime notes](#10-demotime-notes)
11. [Open questions / verify before stage](#11-open-questions--verify-before-stage)

---

## 1. TL;DR — quick comparison matrix

| Dimension | Temporal | Cadence | Conductor | Dapr Workflows | Restate |
|---|---|---|---|---|---|
| **Primary author/steward** | Temporal Technologies (Maxim Fateev, Samar Abbas — ex-Cadence) | Uber | Orkes (donated by Netflix) | CNCF (Microsoft + Diagrid + community) | Restate GmbH (ex-Flink team, Stephan Ewen) |
| **License (server)** | MIT ✅ verified | Apache 2.0 ✅ verified | Apache 2.0 ✅ verified | Apache 2.0 ✅ verified (CNCF graduated) | BSL 1.1 ✅ verified — Change License: Apache 2.0, Change Date: 4 years after each release |
| **Model** | Code-as-workflow, deterministic replay | Code-as-workflow, deterministic replay (same lineage as Temporal) | JSON DSL + code builders; workers in any language | Code-as-workflow (DTFx lineage), runs in Dapr sidecar | Code-as-handler (services / virtual objects / workflows) with journal replay |
| **Primary languages (GA)** | Go, Java, Python, TS, .NET, PHP, Ruby (7 GA) | Go, Java (Python/Ruby community, Python archived) | Python, Java, Go, C#, JS, Clojure, Rust, Ruby | .NET, Python stable; Java beta; Go/JS less mature | TS, Java, Kotlin, Python, Go, Rust |
| **Local dev** | `temporal server start-dev` (single binary) | `docker compose up` (Cassandra + server) | `docker run conductoross/conductor:latest` (single container) | `dapr init` (Docker pulls placement/scheduler/Redis) | `restate-server` (single Rust binary, RocksDB) |
| **Web UI in OSS** | Yes — strong | Yes — functional, less polished | Yes — DAG visualizer is excellent | **No native OSS UI** — Diagrid Dashboard (free, container, requires disclosure) fills the gap | Yes — focused, journal-centric |
| **Managed cloud** | Temporal Cloud (+ BYOC) | Instaclustr (verify); no first-party | Orkes Cloud | Diagrid Catalyst | Restate Cloud |
| **Production storage** | Cassandra / MySQL / Postgres + Elasticsearch | Cassandra (primary) / MySQL / Postgres + ES | Postgres / MySQL / Cassandra / Redis + ES/OpenSearch | Any txn+ETag state store (Postgres / Redis / Cassandra…) via component model | Embedded RocksDB; clustered replicated logs (Bifrost) |
| **Workflow versioning** | `GetVersion` + Worker Deployment Versioning (GA 2025) | `GetVersion` API | Workflow `version` integer (full pinning) | **`IsPatched()` + named versions** since Dapr 1.17 (Feb 27, 2026) — GA in Go/Python/.NET/Java | Code-version via deployment registration |
| **Search / visibility** | Search attributes + ES advanced visibility | Same model as Temporal (ES) | Full-text via ES/OpenSearch | **Limited** — no advanced visibility | SQL introspection endpoint on server |
| **Latest release** (verified 2026-05-16) | v1.31.0 (Apr 29 2026) | v1.4.1-prerelease14 (Apr 14 2025) | v3.30.0.rc15 (May 15 2026) | v1.17.7 (May 15 2026) | v1.6.2 (Feb 13 2026) |
| **GitHub stars** (verified 2026-05-16) | ~20.3k | ~9.3k | ~31.8k | ~25.8k | ~3.9k |
| **Killer demo beat** | Crash-resilience replay in Web UI | Side-by-side with Temporal showing fork lineage | Visual DAG lighting up + multi-language workers | Start a workflow from `curl`; works in any language | `kill -9` mid-workflow + awakeable resolution |
| **One-line bias** | The mature default — broadest SDK coverage, biggest community | The original — still production-grade but losing mindshare to Temporal | Different bucket: workflow-as-*data*, not as-code | Lightest setup; weakest visibility/versioning story | Newest, smallest footprint, most interesting programming model |

---

## 2. Framing the category

### What is "durable execution"?

Code that survives process crashes, host failures, deploys, and indefinite waits — without the developer writing checkpointing or retry plumbing. The runtime persists every observable step (or replays the function to reconstruct state), so a function that calls `sleep(30 days)` actually works.

### "Workflow-as-code" — and an honest caveat

Four of the five platforms (Temporal, Cadence, Dapr, Restate) genuinely fit the workflow-as-code mold: you write a function in a normal language, and the platform makes it durable. **Conductor sits in a different bucket** — its canonical representation is a JSON DSL that defines a DAG of tasks; workers (in any language) execute SIMPLE tasks. Orkes ships code builders in 8+ languages that emit the JSON, but the runtime is still interpreting JSON, not replaying your imperative code. Be explicit about this on stage — it is the single biggest conceptual difference of Conductor from the others, and Conductor's strengths (multi-language workers sharing one workflow, no determinism rules) and weaknesses (no native imperative control flow) flow from it.

### Lineage map (good slide)

```
  Uber 2017 ──── Cadence (Apache 2.0, Uber)
        │
        └── Maxim Fateev & Samar Abbas leave (2019)
                │
                └── Temporal (MIT, Temporal Technologies)

  Microsoft Durable Functions ─── Durable Task Framework (DTFx)
                                         │
                                         └── Dapr Workflows (Apache 2.0, CNCF)

  Netflix 2016 ───── Conductor (Apache 2.0)
        │
        └── Netflix archives (Dec 2023) → conductor-oss/conductor (Orkes-led)

  ex-Apache Flink team (Stephan Ewen) ─── Restate (BSL 1.1, Restate GmbH, 2023)
```

---

## 3. Temporal

### Developer experience

- **Quickstart**: `brew install temporal && temporal server start-dev` — server + Web UI at `localhost:8233` in ~5 seconds.
- **Model**: workflows (deterministic replay) + activities (side-effecting) + signals/queries/updates + child workflows + continue-as-new + timers + side effects + search attributes + schedules + heartbeats.
- **Sandbox**: Python uses an import-restricting sandbox; TS uses an isolated v8 sandbox. Catches non-deterministic calls early.
- **Web UI**: industry-leading — event history with one row per event, stack-trace query for stuck workflows, real-time signal/event view.
- **Replayer**: every SDK ships a replayer; download history with `temporal workflow show --output-filename history.json` and run it locally with breakpoints.
- **Learning cliff**: determinism rules are the steepest part. `learn.temporal.io` is the best official tutorial site of the five platforms.

### Language support & features

| SDK | Latest | Status |
|---|---|---|
| Go | v1.43.0 (Apr 2025) | GA — reference impl |
| Java | v1.35.0 (Apr 2026) | GA — Spring Boot 4 + Spring AI in v1.35 |
| Python | v1.27.2 (May 2025) | GA — pydantic + AsyncIO native |
| TypeScript | v1.17.2 (May 2025) | GA — v8 isolate sandbox |
| .NET | v1.14.1 | GA |
| PHP | v2.17.1 | GA — requires RoadRunner |
| Ruby | v1.4.1 (May 2025) | GA since Sept 2025 |

Feature highlights (2025–2026):
- **Nexus** GA in server v1.31 — cross-namespace/cluster durable RPC primitive.
- **Worker Deployment Versioning** GA in v1.31 (Apr 2025) — pin workflows to a worker build ID; ramp + retire.
- **Standalone Activities** — start an activity without a wrapping workflow.
- **Workflow Streams** (Python v1.27, preview) — durable event channel for AI agent UIs.
- **External payload storage / S3** — offload around the 2 MB payload / 50 MB history limits.

### Hosting

- **Self-host:** Cassandra / MySQL 8+ / Postgres 12+ for persistence; SQL or Elasticsearch for visibility. Cassandra cannot serve visibility. Server = 4 services (frontend, history, matching, worker), each horizontally scalable. Helm chart: `temporalio/helm-charts` v1.2.0.
- **Docker Compose**: legacy repo archived **Jan 5, 2026** — use `temporalio/samples-server/tree/main/compose`.
- **Temporal Cloud**: per-Action pricing (~$25/1M actions, **verify** at temporal.io/pricing) + active storage GB-hours + egress. ~13 AWS regions + GCP regions. Multi-region "Global Namespaces" for active-passive. **BYOC** GA on AWS (GCP preview/GA).
- **SLA**: 99.9% standard / 99.99% HA.

### Performance & scalability

- **Sharding**: history service partitioned into a fixed number of shards (512 / 4096 / 16384). Each workflow hashes to one shard for life. Shards can be increased via migration but not decreased.
- **Public data**: well-tuned single cluster sustains 10k+ state transitions/sec; large customers (Snap, DoorDash, Datadog, Stripe, Box) at 100k+ actions/sec; Cloud namespaces routinely cross billions of actions/month.
- **Benchmark tooling**: `temporalio/omes` (successor to archived `maru`, Oct 2024).
- **Operational pain**: Cassandra + Elasticsearch is the most-cited self-host complaint.

### Community & support

- License: MIT.
- GitHub: ~20.3k stars on `temporalio/temporal`, ~1.6k forks, 200+ contributors.
- Company: Temporal Technologies, Seattle, founded 2019.
- Funding (public): Seed ~$4.7M (2019), A $18.75M (2021, Sequoia), B $103M (2022, Index — $1.5B valuation), C $75M (2023, $1.72B). A 2024/25 extension rumored — **verify**.
- Community: `community.temporal.io` Discourse (very active), Temporal Slack (tens of thousands), annual **Replay** conference.
- Ecosystem 2024–2026: OpenAI Agents SDK, Google ADK, Spring AI, LangSmith tracing, OpenTelemetry v2, AWS Lambda workers, Nexus.

### Demo plan

**Language: Python.** ~25-line single-file demo:

```python
# greet.py
import asyncio
from datetime import timedelta
from temporalio import activity, workflow
from temporalio.client import Client
from temporalio.worker import Worker

@activity.defn
async def greet(name: str) -> str:
    return f"Hello, {name}!"

@workflow.defn
class GreetingWorkflow:
    @workflow.run
    async def run(self, name: str) -> str:
        return await workflow.execute_activity(
            greet, name, start_to_close_timeout=timedelta(seconds=10)
        )

async def main():
    client = await Client.connect("localhost:7233")
    async with Worker(client, task_queue="demo", workflows=[GreetingWorkflow], activities=[greet]):
        result = await client.execute_workflow(
            GreetingWorkflow.run, "Marc", id="demo-1", task_queue="demo"
        )
        print(result)

asyncio.run(main())
```

**Wow moment**: crash-resilience. Add a loop that increments a counter and `workflow.sleep(5)`s. Kill the worker mid-run; restart; workflow picks up exactly where it left off. The Web UI shows replayed events in real time.

**Strengths**: most mature, broadest SDK coverage, best Web UI, real SLAs, Cloud + BYOC.
**Trade-offs**: heaviest self-host footprint (Cassandra + ES), steepest determinism learning curve, action-based Cloud pricing unpredictable for high-fanout workloads.

---

## 4. Cadence

### Framing

**The original.** Created at Uber in 2017 by Maxim Fateev and Samar Abbas (same people who later forked it to start Temporal in 2019). Programming model is virtually identical to Temporal's. If you know one, you know the other. Where they diverged: Temporal modernized the protocol (gRPC + Protobuf from day one; Cadence shipped with TChannel + Thrift, added gRPC later), built more SDKs, and built a commercial company around it. Cadence stayed Uber-internal-first.

**One-liner:** *"Cadence is the original. Temporal is the fork by the original authors that took the spotlight."*

### Developer experience

- **Quickstart**: `git clone github.com/cadence-workflow/cadence && cd docker && docker compose up` → server (gRPC 7833, TChannel 7933), Cassandra, Prometheus, Grafana, Web UI at `localhost:8088`.
- **Model**: workflows + activities + signals + queries + side effects + child workflows + continue-as-new + cron + new **Schedules** (CLI commands added April 2026 in v1.4.1 prereleases) + versioning (`workflow.GetVersion`) + archival to S3/GCS/FS.
- **Web UI**: `cadence-workflow/cadence-web` v4.0.15 (Apr 28, 2026) — Next.js. Functional but less polished than Temporal Web.
- **CLI**: well-developed, on par with `temporal` feature-wise, less ergonomic UX.

### Language support & features

| SDK | Latest | Status |
|---|---|---|
| Go | v1.3.0 (Jul 2025) + active master | First-party, mature |
| Java | **v4.0.0** (Oct 14, 2025) | First-party, mature |
| Python | archived Sep 2023 | **Dead** (`firdaus/cadence-python`) |
| Ruby | sporadic | Community (Coinbase) |

**Honest take**: Cadence is a **Go + Java** product in 2026. For multi-language shops, this is the single biggest reason to pick Temporal over Cadence.

Distinctive features:
- **Active-Active Domains** (v1.4.0, Feb 2025): per-workflow `ActiveClusterSelectionPolicy` — more flexible than Temporal's multi-cluster replication.
- **Shard Distributor Service** (2025): dynamic shard assignment + load balancing (vs static ring-hash).
- **Replication latency** dropped from ~13s → ~2s in 2025 work.

### Hosting

- **Self-host only** (realistically). Cassandra default; MySQL, Postgres, CockroachDB, TiDB also supported. ES/OpenSearch for visibility.
- **Docker images**: `ubercadence/server`, `ubercadence/cli`, `ubercadence/cadence-web`.
- **Helm**: community chart `banzaicloud/cadence-charts`; no official Uber chart.
- **Managed**: Uber doesn't offer one. **Instaclustr (NetApp)** historically offered "Managed Cadence" — **verify status May 2026**. Mindshare is tiny vs Temporal Cloud.

### Performance & scalability

- Architecture identical to Temporal (same code lineage): frontend / history / matching / worker, sharded by `workflowID` hash.
- **At Uber**: hundreds of internal services, multitenant clusters shared by hundreds of apps, billions of workflows executed, tens of thousands of decisions/sec per cluster (public Uber Eng talks). **Pull current numbers from `uber.com/blog/cadence/` before the talk.**
- 2025–2026 perf work: replication latency, in-memory cache (~20% fewer DB calls), caller-type rate limiting.

### Community & support

| | Cadence | Temporal |
|---|---|---|
| Stars | ~9.3k | ~20.3k |
| Forks | 898 | 1.6k |
| Server commits | ~5,534 | ~9,040 |
| Latest release | v1.4.0 (Feb 27, 2025); v1.4.1-prerelease14 (**Apr 14, 2025**) | v1.31.0 (Apr 29, 2026) |
| License | Apache 2.0 | MIT |

- **Status (verified 2026-05-16)**: latest tagged release is **>13 months old**. Project is not archived, but release velocity has slowed dramatically vs Temporal. Uber-internal work may be ahead of public tags — verify with current Uber blog / KubeCon talks before quoting on stage.
- But Temporal has clearly outpaced it on stars, commits, releases, and SDK surface.
- Discussion: GitHub Discussions/Issues, Stack Overflow `cadence-workflow`, CNCF Slack `#cadence-users` (Cadence is not a CNCF project despite using their Slack).
- **Strongest reason to pick Cadence today**: already running it, need Active-Active domains specifically, want Apache-2.0 over MIT, or want to avoid Temporal Cloud commercial gravity.

### Demo plan

```bash
git clone https://github.com/cadence-workflow/cadence
cd cadence/docker && docker compose up -d
docker exec cadence-cli --do test-domain domain register

git clone https://github.com/cadence-workflow/cadence-samples
cd cadence-samples && make
./bin/helloworld -m worker &
./bin/helloworld -m trigger
```

Open `localhost:8088`, drill into the running workflow, show event history.

**Side-by-side payoff**: put a Temporal hello-world Go file next to a Cadence hello-world Go file. The code is nearly identical. Audience sees the lineage instantly.

**Doc honesty disclaimer**: Cadence docs at `cadenceworkflow.io` are noticeably more stale and patchy than Temporal's. Be transparent.

---

## 5. Conductor

### The two-Conductor situation (clarified)

| | `conductor-oss/conductor` | `Netflix/conductor` |
|---|---|---|
| Status | **Active**, primary line | **Archived Dec 13, 2023** |
| Latest | v3.23.0 stable (Mar 2026); v3.30.0.rc15 (May 15, 2026) | v3.15.0 (Nov 2023) |
| Maintainer | Orkes + community | Frozen |
| License | Apache 2.0 | Apache 2.0 |
| Stars | ~32k | ~12.8k |

Use `conductor-oss/conductor` for everything. **OCL is gone from the OSS repo** — verified 2026-05-16 by direct GitHub code search: searching the main branch for `"Orkes Community License"` returns **zero matches**, and the top-level LICENSE is straight Apache 2.0 (copyright "Orkes, Inc.", governed by Apache 2.0). Orkes-only proprietary modules (RBAC, AI Studio, multi-tenant control plane, audit/SAML) live in **Orkes Cloud** / **Orkes Enterprise** only — they are not in the OSS tree.

### Developer experience

Three authoring modes:
1. **JSON DSL** (canonical) — workflows posted to server as JSON.
2. **Orkes UI workflow builder** — drag-and-drop.
3. **Code-based SDK builders** (Python, Java, JS, Go, C#, Clojure, Rust, Ruby) — fluent APIs that **emit JSON under the hood**.

**Important framing**: Conductor is fundamentally workflow-as-**data**, not workflow-as-code. There's no deterministic replay of imperative code; the engine interprets JSON DAGs and workers execute SIMPLE tasks. Code builders are sugar over JSON. Call this out explicitly on stage — it's a real conceptual difference.

- **Quickstart**: `docker run -p 8080:8080 -p 5000:5000 conductoross/conductor-standalone:latest` — all-in-one (in-memory persistence + UI + server) in ~30s. UI at `localhost:5000`.
- **UI**: workflow definition editor (JSON + visual DAG), execution history with per-task drilldown, task queue stats. The DAG view is **visually striking on stage** — fork/join lights up beautifully.

### Language support & features

| Language | Repo | Code-builder? |
|---|---|---|
| Python | `conductor-oss/python-sdk` | Yes (most ergonomic) |
| Java | `conductor-oss/java-sdk` | Yes |
| Go | `conductor-oss/go-sdk` | Yes (functional) |
| C#/.NET | `conductor-oss/csharp-sdk` | Yes |
| JS/TS | `conductor-oss/javascript-sdk` | Yes |
| Clojure | `conductor-oss/clojure-sdk` | Yes |
| Rust | `conductor-oss/rust-sdk` | Worker-focused |
| Ruby | `conductor-oss/ruby-sdk` | Worker-focused |

**System tasks** (engine-executed, no worker code): HTTP, SWITCH, FORK_JOIN, FORK_JOIN_DYNAMIC, SUB_WORKFLOW, DO_WHILE, WAIT, EVENT, INLINE, JSON_JQ_TRANSFORM, HUMAN, KAFKA_PUBLISH, START_WORKFLOW, TERMINATE, SET_VARIABLE, DYNAMIC.

**Worker task**: SIMPLE (your code, polled by SDK worker).

Features: versioning (integer per workflow), retries (FIXED/EXPONENTIAL/LINEAR backoff), rate limiting (per-task + per-workflow concurrent limits), human tasks (native `HUMAN` type with form schema + UI inbox), events/signals (Kafka, SQS, NATS, AMQP, GCP Pub/Sub).

### Hosting

- **Persistence**: Redis, Postgres, MySQL, Cassandra, Dynomite (legacy), in-memory (dev).
- **Indexing**: ES 6/7/8 or OpenSearch 2/3 for workflow search.
- **Recommended modern stack**: Postgres + OpenSearch.
- **Orkes Cloud** — managed multi-region SaaS, free dev tier, paid plans with SLA/RBAC/audit/AI Studio.
- **Resource footprint**: demo ~1 GB RAM all-in-one; production server is stateless Java (4–8 GB heap typical), scales horizontally; bottleneck is always the persistence/index tier.

### Performance & scalability

- **Origin scale**: Netflix ran Conductor for content pipelines, video encoding, studio tooling — millions of workflow executions/day (Netflix Tech Blog 2016+).
- **Architecture quirk**: workers **long-poll** via REST (`/api/tasks/poll/{taskType}`). Adds latency vs Temporal/Cadence's gRPC streaming and creates idle load. Orkes added batch-poll and gRPC poll endpoints recently to mitigate.
- **Orkes benchmarks**: ~10k+ workflow starts/sec, ~100k+ task updates/sec on tuned Postgres+OpenSearch (2024 blog).
- **Bottlenecks**: ES index lag under burst, Redis queue contention, large I/O blowing up DB (mitigated via `EXTERNAL_PAYLOAD_STORAGE` to S3).

### Community & support

- **~31.8k stars** on `conductor-oss/conductor` (verified 2026-05-16), ~894 forks, ~123 open issues, latest release `v3.30.0.rc15` (May 15, 2026) — weekly rc cuts.
- Slack: `orkes-conductor.slack.com` (active).
- Commercial: Orkes (Cloud, Enterprise on-prem, 24/7 support).
- Ecosystem 2025–2026 has heavily focused on **AI agent orchestration** — built-in LLM tasks, RAG primitives, MCP tool calling.
- **Governance caveat**: single-vendor steward (Orkes); not in CNCF (unlike Dapr). Acknowledge this in the talk — same governance shape as Temporal Inc. ↔ Temporal OSS.

### Demo plan

```bash
docker run --rm -d -p 8080:8080 -p 5000:5000 --name conductor \
  conductoross/conductor-standalone:latest
```

Python worker + workflow (~25 lines):

```python
from conductor.client.worker.worker_task import worker_task
from conductor.client.workflow.conductor_workflow import ConductorWorkflow
from conductor.client.workflow.executor.workflow_executor import WorkflowExecutor
from conductor.client.configuration.configuration import Configuration
from conductor.client.automator.task_handler import TaskHandler

config = Configuration(server_api_url="http://localhost:8080/api")

@worker_task(task_definition_name="greet")
def greet(name: str) -> dict:
    return {"message": f"Hello, {name}!"}

@worker_task(task_definition_name="shout")
def shout(message: str) -> dict:
    return {"shouted": message.upper()}

executor = WorkflowExecutor(configuration=config)
wf = ConductorWorkflow(name="hello_demo", version=1, executor=executor)
wf >> greet(task_ref_name="g", name="${workflow.input.name}")
wf >> shout(task_ref_name="s", message="${g.output.message}")
wf.register(overwrite=True)

with TaskHandler(configuration=config, scan_for_annotated_workers=True) as h:
    h.start_processes()
    run = executor.execute(name="hello_demo", version=1, workflow_input={"name": "Marc"})
    print(run.output)
```

**On-stage moments**:
1. Show the generated JSON in **Workflow Definitions → hello_demo v1** — call out "this is what's really running."
2. Execution DAG with both tasks green; click each for input/output, worker id, timestamps.
3. **Kill the worker mid-execution**, restart, workflow resumes — Conductor's durability proof.
4. Bonus: start a Go worker alongside Python on the same task type — multi-language money shot.

**Honest tradeoff to call out**: Conductor's "workflow-as-code" is really "JSON-as-code-builder." Different bucket from Temporal/Dapr/Restate. That's a feature (no determinism rules, trivial multi-language workers) and a trade-off (no native imperative control flow; branching is `SWITCH` + JQ expression).

---

## 6. Dapr Workflows

> **Disclosure**: Marc works at Diagrid (the commercial company behind Dapr). Stated on opening slide.

### Developer experience

- **Model**: workflows-as-an-API on the Dapr sidecar. Workflows authored in code; SDK speaks gRPC to the sidecar; the **sidecar hosts the engine** (built on the Durable Task Framework — same lineage as Azure Durable Functions).
- **Patterns**: task chaining, fan-out/fan-in, child workflows, durable timers, external events, continue-as-new, monitor pattern.
- **External events** delivered via API: `POST /v1.0-alpha1/workflows/dapr/<id>/raiseEvent/<eventName>`.
- **Determinism rules**: same as DTFx/Temporal — no direct I/O, no `time.time()`/`DateTime.Now` (use `ctx.current_utc_datetime`), no random/GUID, no non-deterministic threading. All side effects through activities.

- **Quickstart**: `brew install dapr/tap/dapr-cli && dapr init` — pulls placement, scheduler, Redis via Docker. Run with `dapr run --app-id ... -- python app.py`.
- **Sidecar implications**: two processes per pod (app + sidecar). Workflow engine runs inside each sidecar co-located with the app — your sidecars are collectively the workflow execution layer (not a separate cluster like Temporal). Placement service was a SPOF concern historically; now HA with Raft.

### Language support & features

Current line: **Dapr v1.17.7 (May 15, 2026)**, with v1.18 in RC. Workflow building block has been stable since 1.15 (Jan 2025).

| Language | Status |
|---|---|
| .NET | **Stable**. `Dapr.Workflow` NuGet. Closest API to Durable Functions. |
| Python | **Stable**. `dapr-ext-workflow`. Async/await. |
| Java | **Stable** (workflow versioning supported in 1.17). `io.dapr:dapr-workflows`. |
| Go | **Stable** (workflow versioning supported in 1.17). `github.com/dapr/go-sdk/workflow`. |
| JavaScript | Alpha / experimental. `@dapr/dapr` workflow module. |

Features:
- Child workflows, retries (declarative policies — max attempts, initial interval, backoff coefficient, max interval, retry timeout), external events, parallelism, sub-workflows, continue-as-new.
- **Workflow API endpoints** (HTTP/gRPC): `/v1.0/workflows/dapr/<wf>/start`, `/v1.0/workflows/dapr/<id>` (status), `/raiseEvent/<eventName>`, `/pause`, `/resume`, `/terminate`, `/purge` — **start a workflow from `curl`**, no SDK required. Real differentiator.

#### Workflow versioning — new in Dapr 1.17 (Feb 27, 2026) ⭐

Closes the longest-standing gap vs Temporal. Both pieces — patching **and** named workflow versions — shipped together in **1.17.0 GA** (release notes call out both #9163/#9217 *Workflow versioning (patching)* and #9282/#9291/#9292 *Full workflow versioning*; PR #9292 is the cherry-pick of #9291 into release-1.17). The 1.18.0-rc.1 changelog re-lists #9291 because of cherry-pick branching, **not** because the feature was incomplete in 1.17.

Two complementary mechanisms, used together (concept proposal: [dapr/proposals#94](https://github.com/dapr/proposals/pull/94)):

1. **Patching** — additive `IsPatched("name")` branches inside an existing workflow function. Per-SDK API:
   - .NET: `context.IsPatched("identifier")`
   - Go: `ctx.IsPatched("identifier")`
   - Python: `ctx.is_patched("identifier")`
   - Java: equivalent on the workflow context (verify exact method name in `io.dapr:dapr-workflows`).
   Patches must be append-only — once deployed they can never be moved or removed (deterministic replay constraint).

2. **Named workflow versions** — three distinct SDK idioms:

   | SDK | API |
   |---|---|
   | Go | `registry.AddVersionedWorkflow("Workflow", false, WorkflowV1)` + `AddVersionedWorkflow("Workflow", true, WorkflowV2)` (boolean = `isLatest`) |
   | Python | `@wfr.versioned_workflow(name="workflow", is_latest=True)` |
   | **Java** | `WorkflowRuntimeBuilder.registerWorkflow(name, class, versionName, isLatestVersion)` — overloads with two extra params; backing type `WorkflowVersionWrapper` ([src](https://github.com/dapr/java-sdk/blob/master/sdk-workflows/src/main/java/io/dapr/workflows/runtime/WorkflowVersionWrapper.java)) |
   | **.NET** | **Convention + source generator** — no `AddVersionedWorkflow` call. Wire up with `builder.Services.AddDaprWorkflowVersioning();` + `builder.Services.UseDefaultWorkflowStrategy<NumericVersionStrategy>("workflow-defaults");` + `ConfigureStrategyOptions<NumericVersionStrategyOptions>(o => o.SuffixPrefix = "V");`. Then name your classes `VacationApprovalWorkflow` and `VacationApprovalWorkflowV2` — the `NumericVersionStrategy` discovers them via source generator. ([example](https://github.com/dapr/dotnet-sdk/tree/master/examples/Workflow/WorkflowVersioning), [WorkflowVersionTracker.cs](https://github.com/dapr/dotnet-sdk/blob/master/src/Dapr.Workflow/Versioning/WorkflowVersionTracker.cs)) |

   The .NET model is a **deliberate idiomatic divergence** from Go/Python — worth calling out on stage if the audience is .NET-heavy.

**Migration story**: existing in-flight workflows continue replaying against the version they were started with; new instances run against the latest registered version. No mid-flight version migration (preserves deterministic replay). Recommended pattern: apply patches iteratively until they accumulate, then introduce a named version for a clean refactor.

**Rollout caveat**: workflows can stall during rolling deploys if different replicas have different "latest" versions registered. Stagger or use a deployment versioning strategy.

#### What Dapr 1.18 adds on top (currently in RC, May 2026)

1.18 is the **hardening release** for versioning — *not* the introduction. New workflow entries from [v1.18.0-rc.1](https://github.com/dapr/dapr/releases/tag/v1.18.0-rc.1):

- **Workflow history signing for tamper detection** (#9778) — and termination of tampered in-flight workflows (#9825).
- **Per-workflow-name concurrency limits** (#9775) — global and per-name caps.
- **`WorkflowAccessPolicy` CRD** (#9790) — access-control for workflow operations.
- **Stalled workflow retention/deletion** (#9588, #9612, #9651).
- **ContinueAsNew event-loss fix under high event volume** (#9760).
- **Workflow failure stack traces in API responses** (#9339), **internal events** (#9353), **clear cache when stalled** (#9297), **poll-based completion check replaced** (#9440), **`WorkflowsRemoteActivityReminder` default true** (#9789).

**Slide framing**: 1.17 = "Dapr Workflows now has versioning." 1.18 = "and here are the production-hardening features for it (tamper detection, concurrency caps, access policies)." Don't pitch "Full workflow versioning" as 1.18-only — it shipped in 1.17.0 GA.

#### Docs caveats
The official [workflow-versioning docs page](https://github.com/dapr/docs/blob/v1.17/daprdocs/content/en/developing-applications/building-blocks/workflow/workflow-versioning.md) (v1.17 branch) has **.NET / Go / Python** code samples but **does not yet include a Java sample**, even though the Java SDK supports it. .NET coverage narrates the source-generator approach without a full named-version code sample. Marc may want to file a docs PR before the talk.

**Sources**: [Dapr v1.17.0 release notes](https://github.com/dapr/dapr/releases/tag/v1.17.0), [Dapr v1.18.0-rc.1 release notes](https://github.com/dapr/dapr/releases/tag/v1.18.0-rc.1), [versioning proposal #94](https://github.com/dapr/proposals/pull/94), [PR #9291](https://github.com/dapr/dapr/pull/9291), [PR #9292 cherry-pick into 1.17](https://github.com/dapr/dapr/pull/9292).

**Honest limitations vs Temporal** (post-1.17):
- **History size**: practical limit ~1–2 MB; 10k+ activities in one workflow not recommended without sub-workflow partitioning. Temporal handles much larger histories.
- **No workflow search attributes / advanced visibility.** Can't `SELECT workflows WHERE customerId='x'`.
- **Replay debugger** less polished than Temporal's per-language tools.
- **No local activities equivalent.**
- **No first-party OSS Web UI** — but Diagrid Dashboard (see Hosting) is a free local container that fills this gap; disclose as a Diagrid product on stage.

### Hosting

- **Self-host (dev)**: `dapr init`, Docker-based, Redis default.
- **Self-host (prod)**: sidecars + state store with **transactions + ETag-based concurrency** (hard requirement to back actor engine). Postgres recommended for durability; Redis viable with persistence tuning; MS SQL Server, Cassandra, CockroachDB, Cosmos DB, DynamoDB also work. Components without txn/ETag (plain S3, some etcd configs) **cannot** back workflow.
- **Kubernetes**: Helm chart or Dapr Operator. Sidecar injection via `dapr.io/enabled: "true"` annotation. Control plane: operator, placement, scheduler, sentry, sidecar-injector. HA = 3 replicas each.
- **Diagrid Dashboard** (also called "Local Workflow Dashboard" in the docs — free, local, **disclose as Diagrid product**): a closed-source binary distributed as a public container that reads your local Dapr actor state store directly to visualize workflow instances, inputs/outputs, status, and step-by-step execution history. Replaces the missing first-party OSS Web UI for local development. **Free, fully local, no Diagrid account required.** Docs: [docs.diagrid.io/develop/local-development/dev-dashboard](https://docs.diagrid.io/develop/local-development/dev-dashboard).
  - **Image**: `ghcr.io/diagridio/diagrid-dashboard:0.0.3` (public GHCR, anonymous pull). Pinnable tags currently published: `0.0.1`, `0.0.2`, `0.0.3` (= `latest`). **Pin `0.0.3` for stage reliability** — don't use `:latest` (new push the night before could change behavior). ([GHCR package](https://github.com/orgs/diagridio/packages/container/package/diagrid-dashboard))
  - **Quickstart (Redis, the `dapr init` default)**: `docker run -p 8080:8080 ghcr.io/diagridio/diagrid-dashboard:0.0.3` → UI at `http://localhost:8080/`.
  - **Other state stores**: mount the component YAML and set `COMPONENT_FILE`:
    ```
    docker run -p 8080:8080 \
      -v ./resources/state_postgres.yaml:/app/components/custom_state.yaml \
      -e COMPONENT_FILE=/app/components/custom_state.yaml \
      ghcr.io/diagridio/diagrid-dashboard:0.0.3
    ```
  - **Supported state stores**: Redis (auto-detected), Postgres (manual config), SQLite (manual config). Others not yet supported.
  - **What it shows**: workflow instance list, per-instance drill-down (input, output, status, historical data), expandable **Execution History** table with per-step events.
  - **Integration model**: it **does not** talk to a running Dapr sidecar; it reads the actor state store directly. Works regardless of whether your app was started via IDE, `dapr run`, Aspire, or Kubernetes — as long as the same state store is shared.
  - **Also packaged as**: NuGet `Diagrid.Aspire.Hosting.Dashboard` v0.0.1 (May 5, 2026) — `builder.AddDiagridDashboard();` from a .NET Aspire AppHost. Docker Compose snippets and a Kind/K8s ConfigMap+Deployment+NodePort manifest are also in the docs.
  - **Demo repo**: [`diagrid-labs/dapr-workflow-versioning`](https://github.com/diagrid-labs/dapr-workflow-versioning) — .NET 10 + Aspire orchestrating ApiService, Dapr sidecar, Valkey state store, and the Diagrid Dashboard. Evolves four workflow versions live: **V0** (sequential), **V1** (`IsPatched("AddWeaponsAnalysis")`), **V2** (`DiagnosticsWorkflowV2` fan-out/fan-in — new named workflow), **V3** (adds `WorkflowRetryPolicy` 5 attempts / 3s / 1.5x). V1 stays registered alongside V2 so in-flight V1 instances complete. Requires Dapr CLI 1.17+.
  - **UI version visibility — unconfirmed via web**: `docs.diagrid.io` was blocked from research. Whether the dashboard distinguishes V1 vs V2 instances or surfaces the patch list in the UI **was not verifiable**. **Action**: run the demo locally before the talk, screenshot what the dashboard actually shows, and only assert from direct observation. Don't claim version-aware UI on stage without seeing it.
- **Diagrid Catalyst** (commercial managed Dapr — **disclose**): hosted control plane, managed state stores, **Workflow Explorer UI** in the SaaS console. Distinct from the local Dev Dashboard.
- **Diagrid Conductor** (also Diagrid, separate product — confusingly named, distinct from `conductor-oss/conductor`): Kubernetes management/observability plane for self-hosted Dapr; includes a workflow dashboard for cluster-wide visibility.
- **BYO component model**: swap Redis for Postgres by changing YAML, no code change. Differentiator vs Temporal.

### Performance & scalability

- **Engine model**: each workflow instance is a virtual actor (`dapr.internal.<ns>.<app-id>.workflow`) + an actor per activity. Placement service hashes actor IDs across active sidecars.
- **Throughput scales horizontally with # sidecars** (each sidecar hosts a slice of the actor key-space). Bottleneck is the state store.
- **Scheduler service** (default for workflows in 1.15) replaced older reminders system — substantially improved timer throughput.

**Honest vs Temporal**:
- Temporal has more public benchmark data and operational track record at very high scale (10k+ wf/sec).
- Dapr Workflows is newer (GA Jan 2025); production deployments exist but published large-scale benchmarks are fewer. Community benchmarks in 2024–2025 showed Dapr competitive on small/medium workloads with higher variance under burst.
- **Moderate scale** (hundreds–low-thousands concurrent instances per app): Dapr performs well, smaller operational footprint than Temporal.
- **Planet-scale**: Temporal is the safer bet. Say so on stage.

**Rules of thumb**: keep history <1–2 MB; avoid >~1000 unfinished activities in one workflow; long monitors must continue-as-new on bounded iterations; Postgres for production durability unless Redis ops expertise.

### Community & support

- License: Apache 2.0.
- **CNCF: graduated November 2024**. Only platform of the five with a foundation home.
- Maintainers: Microsoft (largest historical — DTFx engine), **Diagrid** (founded by Dapr's creators Mark Fussell + Yaron Schneider), Alibaba, Intel, plus independents.
- GitHub: `dapr/dapr` ~25k+ stars.
- Components ecosystem: ~100+ across state stores, pub/sub, bindings, secrets.
- Community: Discord, weekly community calls.
- Commercial support: **Diagrid** (Catalyst managed, Conductor self-host mgmt, enterprise contracts), **Microsoft** indirectly via Azure Container Apps Dapr integration + AKS Dapr extension, smaller consultancy ecosystem.

### Demo plan

```bash
brew install dapr/tap/dapr-cli
dapr init
pip install dapr dapr-ext-workflow
```

```python
# app.py
from dapr.ext.workflow import WorkflowRuntime, DaprWorkflowContext, WorkflowActivityContext

def hello_activity(ctx: WorkflowActivityContext, name: str) -> str:
    return f"Hello, {name}!"

def hello_workflow(ctx: DaprWorkflowContext, name: str):
    greeting = yield ctx.call_activity(hello_activity, input=name)
    return greeting

if __name__ == "__main__":
    rt = WorkflowRuntime()
    rt.register_workflow(hello_workflow)
    rt.register_activity(hello_activity)
    rt.start()
    input("Press enter to exit...\n")
    rt.shutdown()
```

```bash
dapr run --app-id wf-demo --dapr-grpc-port 50001 --app-port 0 -- python app.py

# Start from curl — no SDK needed!
curl -X POST "http://localhost:3500/v1.0/workflows/dapr/hello_workflow/start?instanceID=demo1" \
  -H "Content-Type: application/json" -d '"Marc"'

curl http://localhost:3500/v1.0/workflows/dapr/demo1 | jq
```

**On-stage visual options** (Dapr OSS has no native Web UI; pick based on fairness/clarity):
1. **Diagrid Dashboard** (recommended — free local container, disclose as Diagrid product). One command alongside the demo:
   ```
   docker run -p 8080:8080 ghcr.io/diagridio/diagrid-dashboard:0.0.3
   ```
   Open `http://localhost:8080/` to see workflow instances + execution history. Natural pick now that 1.17 versioning makes "watch a new version take over from an old one" a compelling visual moment — start a v1 workflow, deploy v2, start another, show both replaying against their pinned versions in the dashboard.
   Disclosure phrasing: *"To visualize Dapr workflows I'm using the free Diagrid Dashboard container — full disclosure, Diagrid is my employer. It's anonymously-pulled from GHCR, no account required; Dapr OSS doesn't ship a Web UI so this fills the gap locally."*
2. **CLI tail + jq** — `watch -n1 'curl -s localhost:3500/v1.0/workflows/dapr/demo1 | jq'`. Pure OSS, no disclosure needed. Use if the audience seems skeptical of vendor framing.
3. **Zipkin tracing** — `dapr init` ships Zipkin at `localhost:9411`; workflows emit one span per activity. OSS-only fallback.
4. **Diagrid Catalyst Workflow Explorer** — the managed SaaS console. Use briefly as a "what the production-tier looks like" closer, clearly disclosed.

**Where Dapr genuinely shines**: lowest-friction local setup; workflows-as-a-feature inside a broader app platform (state/pub-sub/secrets); polyglot via HTTP API (any language can start/signal without an SDK); Apache 2.0; CNCF graduated; **workflow versioning now first-class (1.17)**.

**Where Dapr is still behind Temporal**: no advanced search/visibility; smaller proven scale ceiling and fewer published large-scale references; JS SDK still maturing; replay debugger less polished per-SDK. The "no first-party Web UI" gap is mitigated (not closed) by Diagrid Dashboard — but it's a Diagrid product, not part of Dapr proper.

---

## 7. Restate

### Programming model — distinctive

Four handler kinds, all on the same durable-execution runtime:

1. **Services** — stateless durable handlers (durable RPC endpoints).
2. **Virtual Objects** — stateful, *keyed* handlers with **single-writer concurrency per key** and durable KV state. Restate's "actor without lifecycle management."
3. **Workflows** — a special virtual object with a `run` handler that executes exactly once per workflow ID, plus auxiliary handlers (signals, queries).
4. **Sagas** — pattern via `ctx.run` + registered compensations (e.g. `ctx.runSaga` in TS/Java — **verify exact API**).

**Restate Context** (`ctx`) is the unifying primitive: `ctx.run` (journaled side-effect), `ctx.sleep` (durable timer), `ctx.awakeable` (external task completion), `ctx.serviceClient` / `ctx.objectClient` (durable RPC), `ctx.get` / `ctx.set` (state).

**Journal-replay model, different from Temporal's**:
- The **Restate server** owns the journal. SDKs are thin protocol clients.
- On failure, server re-invokes the handler; SDK replays journal until reaching an unrecorded step, then makes a real call.
- **The server pushes work to your HTTP handler** — no worker polling a task queue. Handler is just a function reachable at an HTTP URL. Works on Lambda, Cloudflare Workers, K8s, anywhere.
- Determinism rules are *looser* than Temporal's: wrap any non-determinism in `ctx.run(() => ...)`. Every `await` either reads from journal or writes to it ("durable promises").

### Quickstart — single binary

```
brew install restatedev/tap/restate-server
restate-server                                    # RocksDB embedded, ready
restate deployments register http://localhost:9080
```

No Postgres, no Kafka, no Cassandra, no ES. Strongest demo angle vs every other platform.

- **CLI** (`restate`): deployment registration, invocation inspection, state queries, kill/cancel invocations, **SQL queries** against introspection tables, cluster admin.
- **Admin UI** (default port 9070): registered services, in-flight invocations, journal entries per invocation, state per virtual-object key, cancel/kill. Intentionally focused vs Temporal's broader UI.

### Language support & features

| SDK | Latest version (verified 2026-05-16) | Status |
|---|---|---|
| TypeScript / Node | v1.14.3 (May 12, 2026) | GA, most mature. Lambda + Cloudflare Workers supported. |
| Java | (GA) | Spring Boot integrations. |
| Kotlin | (GA) | Coroutine-based; shares Java core. |
| Python | v0.18.0 (May 13, 2026) | GA. `@restate.service` / `@restate.object` / `@restate.workflow`. |
| Go | (GA) | Idiomatic context-passing pattern. |
| **Rust** | **v0.10.0 (Apr 28, 2026)** | **NOT GA** — README explicitly says "currently in active development, and might break across releases." MIT licensed. |

Features:
- **Durable RPC** — `ctx.serviceClient(MyService).myHandler(input)` is journaled end-to-end, caller + callee crash-safe.
- **Awakeables** — `const { id, promise } = ctx.awakeable<T>()`; pass `id` to external system; system POSTs back to `restate.awakeables/<id>/resolve`. Preferred mechanism for "wait for human" / "wait for webhook."
- **Delayed calls / scheduling** — TS: `ctx.objectSendClient(Svc).method(args, restate.rpc.sendOpts({ delay: { hours: 5 } }))`; Python: `send_delay=timedelta(hours=5)` on sends. Deterministic time via `ctx.date.now()` (TS).
- **Sagas — pattern, not API.** The official sagas guide explicitly says *"Define sagas using regular code, no DSLs required."* You maintain a `compensations` list manually and unwind it on `TerminalError`:
  ```ts
  const compensations = [];
  try {
    compensations.push(() => ctx.run("Cancel flight", () => flightClient.cancel(customerId)));
    await ctx.run("Book flight", () => flightClient.book(customerId, flight));
  } catch (e) {
    if (e instanceof restate.TerminalError) {
      for (const compensation of compensations.reverse()) await compensation();
    }
    throw e;
  }
  ```
  Same pattern in Python with `ctx.run_typed` + `TerminalError`. **No `addCompensation` / `runSaga` API exists** — confirmed via SDK source search. ([sagas guide](https://github.com/restatedev/docs-restate/blob/main/docs/guides/sagas.mdx))
- **Cron — pattern, not API.** No first-class cron decorator. Implemented via durable timers + delayed sends, looping a small "scheduler" service. **Official cron guide has TypeScript / Java / Go samples but no Python sample** as of May 2026. ([cron guide](https://github.com/restatedev/docs-restate/blob/main/docs/guides/cron.mdx))
- **Idempotency keys** via inbound HTTP `idempotency-key` header.
- **Kafka ingress** — consume Kafka topics as service inputs.

### Hosting

- **Single binary** (Rust): ingress on 8080, admin API on 9070, worker, log, RocksDB.
- **Kubernetes operator** ([`restate-operator`](https://github.com/restatedev/restate-operator)): **v2.5.1 (Apr 17, 2026)**, 62 releases, actively maintained. Three CRDs with mixed maturity:
  - `RestateCluster` — **v1 (stable)** — the core cluster-provisioning CRD.
  - `RestateDeployment` — `v1beta1` (beta) — declarative service deployment registration.
  - `RestateCloudEnvironment` — `v1beta1` (beta) — Restate Cloud integration.

  Recent additions: Knative Serving mode (v2.2.0), GCP Workload Identity (v2.3.0), trusted CAs + IPv6 (v2.4.0), pod annotations/labels (v2.5.0).
- **Production HA — yes, production-grade in May 2026** (server v1.6.2, Feb 13, 2026). Built on **Bifrost**, a distributed write-ahead log. The log is the source of truth; data is safe as long as you don't lose more than `n` log-server nodes, where `n` is the replication factor.
  - Official recommendation: **replication factor 2** (docs explicitly recommend "at most 2 unless you have strict MTTR requirements").
  - **Minimum 3 nodes** for production (formula `2 * default-replication - 1`) — tolerates one node failure while preserving availability and durability.
  - **Replicated Loglet became the default storage backend** in the v1.4.x series; v1.6.x added automatic partition rebalancing and gossip-based failure detection.
- **Restate Cloud**: managed offering, GA'd 2024. Pricing per active invocation / state size — **verify on stage**.
- **Storage**: embedded RocksDB by default; partitions own slices of state, the Bifrost log is the source of truth.

### Performance & scalability

- **Architecture lineage**: ex-Apache Flink committers (Stephan Ewen co-created Flink). Log-based, partitioned, stateful streaming patterns. Each partition owns a slice of virtual-object keys; state is RocksDB-local to a partition; the log is the source of truth.
- **Footprint**: Rust binary tens of MBs; idle memory in low hundreds of MB. Striking vs a full Temporal cluster.
- **Throughput**: Restate has published benchmarks (e.g. "Performance characteristics of Restate" blog posts) showing tens of thousands of journal appends/sec/node and sub-ms p50 `ctx.run` latencies on local SSD. **Pull fresh numbers the week of the talk — do not quote stale figures.**
- **Scaling model**: horizontal via partitions; each partition single-writer (preserving virtual-object semantics). Add nodes → rebalance partitions.
- **Cold-start friendly**: stateless HTTP handlers → scale-to-zero on Lambda/Cloud Run/Knative works well. Differentiator for serverless audiences.

### Community & support

- **Company**: Restate GmbH, founded 2023, Berlin.
- **Founders**: Stephan Ewen (Apache Flink co-creator, ex-Ververica CTO) + ex-Flink/Ververica team. Strong credibility.
- **Funding**: seed 2023, Series A 2024 (~$25M, **verify**).
- **License**: **BSL 1.1** ✅ verified 2026-05-16 — Licensor: Restate Software, Inc. + Restate GmbH; **Change License: Apache 2.0**; **Change Date: 4 years after each release** (so older releases auto-convert to Apache 2.0 on a rolling basis; nothing has yet converted since the project started in 2023). Additional Use Grant permits any use *except* offering a "Public Restate Platform Service." **SDK repos are separately MIT/Apache 2.0** (verify per-SDK before stage).
- **GitHub**: `restatedev/restate` **~3.9k stars** (verified 2026-05-16) — growing fast for its age; smaller than the others.
- **Community**: Discord (active, Restate engineers participate directly). Smaller than Temporal's but punches above its weight per-capita.
- **Releases**: every 2–4 weeks through 2025.

### Demo plan

1. `restate-server` in one terminal (one command, no config).
2. TypeScript service file `greeter.ts` showing all three primitives:
   - **Service** `greet(name)` calling external API via `ctx.run`.
   - **Virtual Object** `Counter/{userId}` with `increment`/`get` showing keyed state.
   - **Workflow** `SignupWorkflow` with `run` that sleeps, calls a service, then waits on `awakeable` for email confirmation.
3. `npm run dev` to start handler on `localhost:9080`.
4. `restate deployments register http://localhost:9080`.

**Striking moments**:

- **Best beat: `kill -9` mid-flight.** Start `SignupWorkflow` via `curl`. It sleeps 10s. During the sleep, `kill -9` the Node process. Restart it. Restate **automatically re-invokes** as soon as the service is back; journal shows the sleep was already complete; execution resumes from the next step. Admin UI shows journal entries lighting up.

- **Second beat: awakeable.** Workflow parks on `ctx.awakeable()`. Show the awakeable ID in the UI. `curl -X POST .../awakeables/<id>/resolve -d '{"confirmed": true}'` (or QR code for the audience). Workflow completes.

- **Third beat: contrast.** Open Temporal docker-compose alongside — 6 containers vs 1 binary. Visceral.

**Verbal talking points**:
- "There's no worker polling a task queue. Restate calls *your* HTTP handler."
- "The journal lives in the server. The SDK is a protocol client."
- "Virtual objects give actor-like single-writer concurrency, no actor lifecycle to manage."
- "One binary. Rust. RocksDB embedded."

---

## 8. Cross-cutting comparison per dimension

### 8.1 Developer experience

| | Temporal | Cadence | Conductor | Dapr | Restate |
|---|---|---|---|---|---|
| Quickstart wall-clock | <1 min (single binary) | 3–5 min (docker compose, pulls Cassandra) | <1 min (single Docker image) | 1–2 min (`dapr init` pulls 3 containers) | <1 min (single Rust binary) |
| Programming model intuitiveness | High but determinism cliff | Same as Temporal | High *if* you accept JSON DSL | High; familiar to Durable Functions devs | High; loosest determinism rules |
| First-class Web UI in OSS | ✅ best-in-class | ✅ functional | ✅ visually striking DAG | ❌ (use CLI/Zipkin or commercial UI) | ✅ focused, journal-centric |
| Replayer for debugging | ✅ per-SDK | ✅ (Go/Java) | N/A (no replay model) | ✅ (.NET strongest) | ✅ via journal in UI |
| Learning curve | Steep (determinism) | Steep (same) | Shallow (no determinism rules; learn the DSL) | Moderate | Moderate (different mental model) |

### 8.2 Language support — first-party GA SDKs

| Platform | Languages |
|---|---|
| Temporal | Go, Java, Python, TS, .NET, PHP, Ruby (7) |
| Cadence | Go, Java (2); Python community archived |
| Conductor | Python, Java, Go, C#, JS, Clojure, Rust, Ruby (8 — including code builders) |
| Dapr Workflows | .NET, Python (stable), Java (beta), Go (beta), JS (alpha) |
| Restate | TS, Java, Kotlin, Python, Go, Rust |

**Slot 1** for "most polyglot": Conductor (by count) — but those SDKs build JSON, not code workflows. **Slot 1** for "polyglot workflow-as-code": Temporal.

### 8.3 Hosting

| | Self-host complexity | Production storage | Managed cloud | K8s story |
|---|---|---|---|---|
| Temporal | High (Cassandra/SQL + ES) | Cassandra/MySQL/Postgres + ES | Temporal Cloud (+BYOC AWS GA) | Official Helm |
| Cadence | High (Cassandra default) | Cassandra/MySQL/Postgres + ES | Instaclustr only (verify) | Community Helm (`banzaicloud`) |
| Conductor | Medium (server + Postgres/Redis + ES) | Postgres/MySQL/Cassandra/Redis + ES/OS | Orkes Cloud | Community |
| Dapr | Low (sidecar + state store) | Any txn+ETag store (Postgres, Redis, …) | Diagrid Catalyst | Operator / Helm |
| Restate | **Lowest** (single binary) | Embedded RocksDB; Bifrost replicated log for HA (3 nodes / RF 2) | Restate Cloud | Operator v2.5.1 — `RestateCluster` v1 stable; `RestateDeployment` / `RestateCloudEnvironment` beta |

### 8.4 Performance & scalability

| | Proven scale | Architecture | Bottleneck |
|---|---|---|---|
| Temporal | 100k+ actions/sec at large customers; billions/month on Cloud | Sharded 4-service cluster | Cassandra + ES ops |
| Cadence | Uber: hundreds of services, billions of workflows, 10k+ decisions/sec/cluster | Identical to Temporal (lineage) | Same as Temporal |
| Conductor | Netflix scale historically; Orkes ~10k starts/sec, ~100k task updates/sec | Stateless Java server + persistence | Worker polling overhead; ES index lag |
| Dapr Workflows | Moderate (low-thousands concurrent per app); few large public benchmarks | Virtual actors on sidecars | State store |
| Restate | Tens of thousands of journal appends/sec/node (single node); cluster benchmarks evolving | Log-based, partitioned, Rust core | Disk I/O on RocksDB |

### 8.5 Community & support

All license / star figures verified via GitHub LICENSE files and repo homepages on 2026-05-16.

| | License | Stars | Foundation | Commercial |
|---|---|---|---|---|
| Temporal | MIT | ~20.3k | None | Temporal Technologies (Cloud, BYOC, support, SLAs) |
| Cadence | Apache 2.0 | ~9.3k | None (uses CNCF Slack) | Instaclustr (verify) |
| Conductor | Apache 2.0 | ~31.8k | None | Orkes (Cloud, Enterprise) |
| Dapr | Apache 2.0 | ~25.8k | **CNCF Graduated** | Diagrid (Catalyst, Conductor, Dashboard) + Microsoft (Azure Container Apps) |
| Restate | BSL 1.1 (Change License = Apache 2.0, Change Date = 4y) | ~3.9k (fastest growing) | None | Restate GmbH (Cloud) |

---

## 9. Recommended 50-minute talk structure

**Total**: 50 minutes. ~8 min per platform demo + intro/outro/comparison.

| Time | Segment | Mode |
|---|---|---|
| 00:00 – 03:00 | **Opening + disclosure.** What is durable execution? Why does it matter? Diagrid disclosure. Lineage map slide. | Slides |
| 03:00 – 04:00 | **The 5 dimensions** — short slide previewing what we'll evaluate on. | Slides |
| 04:00 – 12:00 | **Temporal** — overview slide + live demo (hello workflow → kill worker → resume). | Slides + DemoTime |
| 12:00 – 18:00 | **Cadence** — short overview, lineage callout, side-by-side Go code with Temporal, brief demo. | Slides + DemoTime |
| 18:00 – 26:00 | **Conductor** — workflow-as-data framing, DAG UI demo, kill-worker proof, multi-language teaser. | Slides + DemoTime |
| 26:00 – 34:00 | **Dapr Workflows** — sidecar model slide, `dapr init` + Python demo, **1.17 workflow versioning** (patch + named-version, side-by-side replay) in the Diagrid Dashboard (disclosed), `curl` from any language. | Slides + DemoTime |
| 34:00 – 42:00 | **Restate** — programming model slide (3 primitives), single-binary demo, `kill -9` payoff, awakeable resolution. | Slides + DemoTime |
| 42:00 – 47:00 | **Cross-platform comparison matrix slide** + decision guide ("If you want X, pick Y"). | Slides |
| 47:00 – 50:00 | **Wrap-up + Q&A pointer + repo link.** | Slides |

### Decision-guide slide (suggested)

- **Need the most mature, broadest-language, biggest community?** → **Temporal**.
- **Already running Cadence at Uber-like scale, or specifically need active-active per-workflow?** → **Cadence**.
- **Workflows defined by non-coders / approval-heavy / many task types out-of-the-box / AI orchestration?** → **Conductor**.
- **Already using Dapr for state/pub-sub, want lowest-friction polyglot, value CNCF governance, want workflow versioning out of the box (1.17)?** → **Dapr Workflows**.
- **Want the smallest operational footprint, novel programming model, serverless-friendly handlers?** → **Restate**.

---

## 10. DemoTime notes

DemoTime drives the session through pre-scripted steps. Five demo apps, one per platform, each ~5–7 minutes of run time.

### Shared structure for each demo (consistency for the audience)

Every platform's demo should hit the same beats so the audience can compare apples-to-apples:

1. **Show the code** — minimal "hello" workflow + 1 activity (~25 lines max).
2. **Start the runtime** — single command (or compose-up).
3. **Run the workflow** — show success.
4. **Inspect** — open the UI (or CLI/Zipkin for Dapr).
5. **Kill-and-resume** (the durability proof) — kill the worker/sidecar/handler, restart, show resumption.
6. **One platform-specific "wow"** moment (different per platform).

### Per-platform DemoTime scripts

| Platform | Step file location (suggested) | "Wow" beat |
|---|---|---|
| Temporal | `demos/01-temporal/demo.json` | Crash-resilience replay in Web UI |
| Cadence | `demos/02-cadence/demo.json` | Side-by-side Go diff with Temporal — show how nearly identical |
| Conductor | `demos/03-conductor/demo.json` | DAG visualization + Go worker alongside Python worker on same task type |
| Dapr | `demos/04-dapr/demo.json` | **Live workflow versioning (1.17)**: start v1, deploy v2, start another — show both replaying against pinned versions in the Diagrid Dashboard (disclosed). Bonus: start a workflow via `curl` to show SDK-less interaction. |
| Restate | `demos/05-restate/demo.json` | `kill -9` mid-flight + awakeable resolved by audience QR-code POST |

### Practical tips

- **Pre-pull all Docker images** the day before the talk; first-time pulls of Cassandra (Cadence/Temporal) and Elasticsearch (Conductor) can take minutes and kill momentum.
- **Pin all versions** in compose files — don't let `:latest` break the demo because of a release the night before.
- **Have a recorded video fallback** for each demo (DemoTime supports this) — conference Wi-Fi has killed more demos than any bug.
- **One shared backing service** — if all demos need a fake external API to call from activities, run one local mock (e.g. `httpbin` on `:8088`) that every demo calls. Saves time and is honest about the platforms doing the same thing.
- **Pick consistent demo input**: a "process payment for Marc" workflow concept across all five lands the comparison better than 5 different toy examples.

---

## 11. Open questions / verify before stage

### Resolved in round-2 research (2026-05-16)

All confirmed via direct WebFetch of GitHub repos / docs.

- [x] All five licenses (Temporal MIT, Cadence Apache 2.0, Conductor Apache 2.0, Dapr Apache 2.0, Restate BSL 1.1).
- [x] All five latest releases + star counts (see TL;DR table).
- [x] **Dapr workflow versioning shipped fully in 1.17.0 GA** (Feb 27, 2026) — both patches and named workflow versions. The "Full workflow versioning" entry in 1.18.0-rc.1 changelog is a cherry-pick re-listing of the same PR; not a deferred feature.
- [x] **Dapr 1.18 delta** = hardening: history signing for tamper detection (#9778) + tampered-workflow termination (#9825), per-name + global concurrency limits (#9775), `WorkflowAccessPolicy` CRD (#9790), stalled-retention cleanup (#9588), ContinueAsNew event-loss fix under high event volume (#9760), internal events, failure stack traces in API.
- [x] **.NET versioning API** = convention + source generator (`AddDaprWorkflowVersioning()` + `UseDefaultWorkflowStrategy<NumericVersionStrategy>(...)`, name classes `WorkflowName` / `WorkflowNameV2`).
- [x] **Java versioning API** = `WorkflowRuntimeBuilder.registerWorkflow(name, class, versionName, isLatestVersion)` overloads.
- [x] **Diagrid Dashboard pinnable tag**: published `0.0.1`, `0.0.2`, `0.0.3` (= `latest`). Use `0.0.3` on stage.
- [x] **Diagrid Dashboard demo repo confirmed**: `diagrid-labs/dapr-workflow-versioning` evolves V0→V1 (`IsPatched("AddWeaponsAnalysis")`)→V2 (new named workflow, fan-out/fan-in)→V3 (adds `WorkflowRetryPolicy`). .NET 10 + Aspire + Valkey + dashboard side-by-side.
- [x] **Restate Rust SDK** is **NOT GA** — v0.10.0 (Apr 28, 2026); README explicitly says "active development, might break across releases."
- [x] **Restate K8s operator** = v2.5.1 (Apr 17, 2026); `RestateCluster` CRD v1 (stable), `RestateDeployment` + `RestateCloudEnvironment` v1beta1 (beta).
- [x] **Restate production HA** = production-grade in May 2026; Bifrost replicated log; recommended replication factor 2; minimum 3 nodes.
- [x] **Restate sagas + cron** are documented *patterns* (compensations list + `ctx.run` + `TerminalError`; delayed sends with `restate.rpc.sendOpts({ delay })`); no `runSaga` / `@cron` API exists. Python has no official cron sample in the docs.
- [x] **Conductor OCL** is **not present** anywhere in `conductor-oss/conductor` main branch — GitHub code search for "Orkes Community License" returned zero matches; top-level LICENSE is Apache 2.0.

### Marc must verify manually (non-GitHub commercial domains blocked from sandbox)

These items live on vendor websites the research sandbox could not reach (temporal.io, uber.com, instaclustr.com, orkes.io, crunchbase.com, restate.dev, docs.diagrid.io). Block out ~30 minutes the week of the talk to check them on your own browser:

#### Temporal
- [ ] **Temporal Cloud pricing** — current per-Action rate, storage GB-hour, egress, free tier. `temporal.io/pricing`.
- [ ] **Funding** — any 2024–2026 Temporal Technologies extension round. Crunchbase / TechCrunch.

#### Cadence
- [ ] **Current Uber scale numbers** — `uber.com/blog/cadence/`, recent KubeCon NA 2025 / QCon 2025 talks (YouTube search). Confirm before quoting throughput or workflow count.
- [ ] **Instaclustr (NetApp) Managed Cadence** — still GA, EOL, deprecated? `instaclustr.com/platform/managed-cadence/`.
- [ ] **Cadence main-branch activity** — latest tagged release is April 2025; check whether main has unreleased work worth referencing.

#### Conductor
- [ ] **Orkes Cloud** — regions, free-tier limits, paid plans. `orkes.io/pricing`, `orkes.io/cloud`.

#### Dapr / Diagrid
- [ ] **Diagrid Dashboard UI version visibility** — when V1 and V2 instances of the same logical workflow are running, does the dashboard distinguish them? Does it show the patch list? **Run the demo locally and observe — don't claim version-aware UI on stage without seeing it.**
- [ ] **Diagrid Dashboard docs** — `docs.diagrid.io/develop/local-development/dev-dashboard` and `/develop/diagrid-dashboard/` for any features / limits not covered in the README.

#### Restate
- [ ] **Latest benchmark numbers** — `restate.dev/blog/` for posts on TPS / latency. Don't quote stale numbers; if the most recent benchmark post is >6 months old, just describe the architecture qualitatively.

### Cross-cutting
- [ ] **Lineage-map slide accuracy** — any new forks / donations / acquisitions in 2025–2026?
- [ ] **Three Diagrid products** — Dashboard (free local), Catalyst (managed SaaS), Conductor (self-host mgmt) — keep distinct disclosure phrasing for each. Audience confusion likely if they're conflated.
