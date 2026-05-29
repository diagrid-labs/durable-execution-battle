# Temporal (Java) — Enterprise Diagnostics Demo

Stage demo #2 in the **Durable Execution Battle** talk. Workflow shape, severity thresholds, and bridge-message format match the canonical scenario in [`../shared/scenario.md`](../shared/scenario.md). The .NET reference at [`../04-dapr/`](../04-dapr/) is the scenario source of truth.

## Stack

- Java 21 LTS
- Spring Boot 3.4.x
- `io.temporal:temporal-sdk` 1.30.x + `temporal-spring-boot-starter` 1.30.x (the `-alpha` line was discontinued at 1.23.2; the GA starter ships dual Spring-Boot-2/3 auto-config)
- Temporal CLI dev server (single binary, in-memory; Temporal Web bundled at `:8233`)

## Run on stage (3 terminals)

```bash
# 1) Temporal dev server (gRPC :7233, Web UI http://localhost:8233)
temporal server start-dev

# 2) Worker + REST API (one process)
cd demos/02-temporal && ./mvnw spring-boot:run

# 3) Kick off the workflow
curl -X POST http://localhost:8080/start \
  -H 'Content-Type: application/json' \
  -d '{"id":"enterprise-ncc-1701","shipName":"USS Enterprise NCC-1701-D","diagnosticsDate":"2370-04-08","engineerName":"Geordi La Forge"}'
```

Watch event history live at <http://localhost:8233>.

## On-stage choreography

1. `WorkflowExecutionStarted`
2. 5 × `ActivityTaskScheduled` / `…Started` / `…Completed` — the fan-out, near-instant
3. `TimerStarted` (60 s)
4. **Marc Ctrl+C the Spring Boot app** at ~T+20 s into the timer
5. **Marc restart `./mvnw spring-boot:run`** — worker reconnects; timer continues to tick on the Temporal server
6. `TimerFired`
7. `PrioritizeIssuesActivity` scheduled + completed
8. `NotifyBridgeActivity` scheduled + completed (severity is rigged to `red_alert`)
9. `WorkflowExecutionCompleted`

The Spring Boot app hosts worker + REST API in one process, so killing the app *is* killing the worker — exactly the durability demo beat.

## Cleanup

`Ctrl+C` both terminals — the Spring Boot app (Terminal 2) and the `temporal server start-dev` process (Terminal 1). No Docker is involved: the dev server is a single binary holding all state in memory, so it leaves nothing behind once stopped.

## Endpoints

| Method | Path | Notes |
|---|---|---|
| `POST` | `/start` | Starts a workflow with `WorkflowId = body.id`. Returns `{ workflowId }`. |
| `GET` | `/status/{id}` | Returns `{ workflowId, status, output? }`. `output` is present only when the workflow has completed. |
| `POST` | `/terminate/{id}` | Forcibly terminates. |
| `POST` | `/cancel/{id}` | Sends a cancellation signal. **Note:** Temporal does not have first-class pause/resume the way Dapr does — `cancel` is the closest analog and the workflow does not resume. |

## Idiomatic-Java notes

- Workflow code (`DiagnosticsWorkflowImpl`) runs in Temporal's deterministic sandbox: no `Random`, no `LocalDateTime.now()`, no `Thread.sleep`. Use `Workflow.sleep`, `Workflow.getLogger`, and put all side-effects in activities.
- Activities are plain Spring beans (`@Component`) — `@ActivityImpl` ties them to the `diagnostics-tq` task queue, and the Spring Boot starter auto-registers them when the worker boots.
- `Severity` serializes to / deserializes from `green` / `yellow_alert` / `red_alert` via `@JsonProperty` to match the .NET wire format.
- Activity interfaces use `@ActivityMethod(name = "Scan{Subsystem}Activity")` (etc.) so the activity type names in Temporal Web match the .NET class names — useful for cross-framework slide comparisons.

## Tests

```bash
cd demos/02-temporal && ./mvnw test
```

Includes:
- `SeverityJsonTest`, `ModelSerializationTest` — wire-format guards
- `MockAnalysisGeneratorTest` — property tests over the biased-low random generator
- `PrioritizeIssuesActivityImplTest` — severity threshold rules
- `NotifyBridgeActivityImplTest` — canonical bridge message format
- `DiagnosticsWorkflowImplTest` — uses `TestWorkflowEnvironment` to exercise fan-out + conditional notify (60 s timer is skipped by the test clock). Activity stubs are lambda implementations of the activity interfaces; Mockito mocks don't work here because Temporal validates that `@ActivityMethod` lives on an interface method, and Mockito subclasses promote them to concrete methods.
