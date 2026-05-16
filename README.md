# Durable Execution Battle — Conference Talk Repo

Source material for a 50-minute conference talk comparing five durable-execution frameworks: **Temporal**, **Cadence**, **Conductor**, **Dapr Workflows**, and **Restate**.

> **Disclosure:** the speaker (Marc Duiker) works at [Diagrid](https://www.diagrid.io/), the commercial company behind Dapr. The talk flags Dapr's limitations alongside its strengths.

## What's here

```
.
├── research.md            # source-of-facts dossier (already drafted)
├── plan.md                # the talk + repo plan (authoring source of truth)
├── slides/                # markdown slide deck — DemoTime-compatible
├── .demo/                 # DemoTime scripts (VS Code extension)
├── .engage/               # EngageTime poll definitions
└── demos/
    ├── shared/scenario.md # the shared USS Enterprise diagnostics spec
    └── 04-dapr/           # Dapr Workflow demo (.NET 10 + Aspire) — vendored from diagrid-labs/dapr-workflow-versioning
```

Demos for Temporal, Cadence, Conductor, and Restate will be authored under separate plans; only the Dapr demo is staged in this first pass.

## Tooling

- **[DemoTime](https://marketplace.visualstudio.com/items?itemName=eliostruyf.vscode-demo-time)** — VS Code extension that drives the on-stage choreography (opens files, runs terminal commands, sends keys, advances slides, etc.). `.demo` files describe ordered steps.
- **EngageTime** — companion extension that runs live audience polls; DemoTime triggers `engageTime.startPoll` / `engageTime.stopPoll` so the speaker only operates one extension on stage.

## Talk timeline (50 min)

See [`plan.md` §1](./plan.md) for the per-segment table. Top-level flow:

1. Opening (3 min) → opening poll (2 min) → lineage + dimensions preview (1 min)
2. Five dimensions of comparison (DX → Lang → Hosting → Perf), 3 min each
3. **Demo block — 25 min**, USS Enterprise diagnostics in all 5 frameworks
4. Community & Support dimension (3 min)
5. Closing poll (1.5 min) → decision guide (1.5 min) → wrap (1 min)

## Demo scenario

Every framework runs the same `DiagnosticsWorkflow`: fan-out 5 parallel subsystem scans → fan-in → 60-second sleep (the kill point) → prioritize → conditional bridge notify. See [`demos/shared/scenario.md`](./demos/shared/scenario.md).

## Status

| Asset | State |
|---|---|
| `research.md` | Drafted |
| `plan.md` | Drafted |
| Slides | Drafted (markdown, ready to render via DemoTime) |
| EngageTime polls | Drafted |
| Dapr demo | Vendored as-is; **needs adaptation pass** to match the shared scenario (5 scans, 60s timer, conditional notify). See [`demos/04-dapr/README.md`](./demos/04-dapr/README.md). |
| Temporal / Cadence / Conductor / Restate demos | Not started — separate plans |
| DemoTime `.demo` scripts | Opening poll, Dapr, closing poll authored. Others pending demo builds. |

## License

MIT. See [`LICENSE`](./LICENSE).
