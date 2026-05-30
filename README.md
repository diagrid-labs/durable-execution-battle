# Durable Execution Battle — Conference Talk Repo

Source material for a conference talk comparing five durable-execution frameworks: **Cadence**, **Temporal**, **Conductor**, **Dapr Workflows**, and **Restate** — by running the *same* workflow in all five and killing it mid-run to prove durability.

## Talk agenda

1. **What is durable execution** — definition + a Star Trek hook (`await sleep(60s)` that survives a crash)
2. **Overview of the five engines** — lineage map (Cadence → Temporal fork) + one-line positioning each
3. **🗳️ Opening poll** (EngageTime) — "Which have you used?"
4. **Demos** — the same `DiagnosticsWorkflow` in all five frameworks, with a live kill-and-resume
5. **Local performance** — a burst of 100 workflows against each engine, single-node local stacks
6. **Evaluate** across five dimensions — Developer Experience, Languages & Features, Hosting & Production, Community & Support
7. **🗳️ Closing poll** (EngageTime) — "Which would you try first?"
8. **Key takeaways & resources**


## Highlights

- **One scenario, five frameworks.** Every demo runs the identical USS Enterprise `DiagnosticsWorkflow`: fan-out 5 parallel subsystem scans → fan-in → a 60-second durable sleep (**the kill point**) → prioritize → conditional bridge alert. See [`demos/shared/scenario.md`](./demos/shared/scenario.md).
- **Kill-and-resume on stage.** During the 60s sleep the worker process is killed; on restart the workflow durably wakes, prioritizes, and fires the 🚨 bridge alert inside each framework's own UI / journal.
- **Deliberate language pairings.** Cadence + Temporal both in **Java** (so the API ancestry shows side-by-side), Conductor + Dapr both in **C#/.NET** (so *workflow-as-data* vs *workflow-as-code* is the only variable). Restate is **TypeScript**.
- **Live performance numbers.** A shared TypeScript driver bursts 100 workflows at each engine; results land in [`slides/05-demo/06-results.md`](./slides/05-demo/06-results.md).

## The demos

All five framework demos live under [`demos/`](./demos), each self-contained with its own `README.md` (stack, prerequisites, run-on-stage steps, and visual cues). The **Dapr/.NET** demo is the canonical scenario reference; the other four mirror its activity names, model shapes, severity thresholds, and rigging.

| # | Framework | Language / Stack | README |
|---|---|---|---|
| 01 | **Cadence** | Java 21 · Spring Boot 3 · `cadence-client` · Cassandra + Cadence (Docker) | [`demos/01-cadence/README.md`](./demos/01-cadence/README.md) |
| 02 | **Temporal** | Java 21 · Spring Boot 3 · `temporal-spring-boot-starter` · Temporal CLI dev server | [`demos/02-temporal/README.md`](./demos/02-temporal/README.md) |
| 03 | **Conductor** | .NET 10 · ASP.NET Core minimal API · `conductor-csharp` · `conductor-standalone` (Docker) | [`demos/03-conductor/README.md`](./demos/03-conductor/README.md) |
| 04 | **Dapr Workflows** *(canonical)* | .NET 10 · Aspire · Valkey state store · Diagrid Dashboard | [`demos/04-dapr/README.md`](./demos/04-dapr/README.md) |
| 05 | **Restate** | Node 22 · TypeScript · `@restatedev/restate-sdk` · single `restate-server` binary | [`demos/05-restate/README.md`](./demos/05-restate/README.md) |

**Performance driver** — [`demos/perf/`](./demos/perf/README.md) runs N copies of the shared workflow against one engine at a time (with the 60s wait skipped) and writes the aggregate table into the results slide. See [`demos/perf/README.md`](./demos/perf/README.md).

## Slides, polls, and demo scripts

- **[`slides/`](./slides)** — the markdown slide deck (DemoTime-compatible), organized by section: `00-open`, `01-dx`, `02-languages`, `03-hosting-production`, `05-demo`, `06-community`, `07-wrap`.
- **[`.demo/`](./.demo)** — DemoTime YAML scripts that drive the on-stage choreography (open files, run terminal commands, send keys, advance slides, trigger polls). One per framework demo, per dimension, and per performance run, plus the opening/closing polls.
- **[`.engage/`](./.engage)** — EngageTime poll definitions (`opening-poll.json` multi-select; `closing-poll.json` single-select). DemoTime triggers `engageTime.startPoll` / `engageTime.stopPoll` so the speaker only operates one extension on stage.

## Tooling

- **[DemoTime](https://marketplace.visualstudio.com/items?itemName=eliostruyf.vscode-demo-time)** — VS Code extension that renders the slides and drives the demos. `.demo/*.yaml` files describe ordered steps.
- **EngageTime** — companion VS Code extension that runs the live audience polls, triggered from DemoTime.

## Learn more

- Learn Dapr Workflow for free with [Dapr University](https://www.diagrid.io/dapr-university).
- Join the [Dapr Discord](https://diagrid.ws/dapr-discord) and discuss workflows in the #workflow channel.