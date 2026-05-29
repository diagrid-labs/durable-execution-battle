# Cadence (Java) — USS Enterprise diagnostics

One of five framework demos for the *Durable Execution Battle* talk. Cadence is the ancestor; Temporal forked from it. This demo's stage purpose is to show the lineage — same SDK surface (`@WorkflowMethod`, `Async.function`, `Promise.allOf`, `Workflow.sleep`) and the cost-of-entry delta (manual `@Bean` wiring + Cassandra-backed cluster) vs Temporal's `start-dev` + Spring Boot starter.

## Stack

- Java 21 · Maven · Spring Boot 3.4.0
- `com.uber.cadence:cadence-client:3.13.0`
- Cadence cluster via `docker-compose.yml` (Cassandra 4.1 + `ubercadence/server:v1.2.18-auto-setup` + `ubercadence/web:v4.0.15`)

## Prerequisites

- Java 21 (`java -version`)
- Docker + Docker Compose
- ~2 GB free RAM for Cassandra
- Optional: `jq` for pretty-printing JSON responses

## One-time pre-talk setup

```
docker pull cassandra:4.1
docker pull ubercadence/server:v1.2.18-auto-setup
docker pull ubercadence/web:v4.0.15
./mvnw dependency:go-offline
```

## Run the demo

Three terminals.

### Terminal 1 — cluster
```
docker compose up
```

### Terminal 2 — Spring Boot app (worker + REST API on port 8080)
```
./mvnw spring-boot:run
```
Look for these log lines:
- `Started EnterpriseDiagnosticsApplication`
- `Cadence domain 'default' already exists` (auto-setup pre-registered it; idempotent)

### Terminal 3 — start the workflow
```
curl -X POST http://localhost:8080/start \
  -H 'Content-Type: application/json' \
  -d '{
    "id": "enterprise-ncc-1701",
    "shipName": "USS Enterprise NCC-1701-D",
    "diagnosticsDate": "2370-04-08",
    "engineerName": "Geordi La Forge"
  }'
```

Then watch Cadence Web at <http://localhost:8088> → `default` domain → workflow `enterprise-ncc-1701`.

## Kill-and-resume choreography

This is the demo's payoff.

1. Hit `POST /start`. Cadence Web shows 5 scan activities complete in ~2 s.
2. Workflow enters a 60-second durable timer (the kill point).
3. **At ~T+20 s into the timer:** `Ctrl+C` the Spring Boot app in Terminal 2.
4. Cadence Web shows the workflow still `Running`, history frozen on `TimerStarted`. The Cadence server is still holding the timer.
5. Restart: `./mvnw spring-boot:run`.
6. When the 60-second timer fires (it keeps ticking through worker downtime), the new worker picks up the decision task immediately. `PrioritizeIssuesActivity` runs; severity reliably hits `red_alert`; `NotifyBridgeActivity` fires.
7. Terminal 2 prints the `🚨 BRIDGE ALERT — USS USS Enterprise NCC-1701-D — RED_ALERT: …` banner at WARN level.

Verified Task 14 outcome: 10/10 reruns landed `red_alert`; the rigging is solid.

## Cleanup

`Ctrl+C` the Spring Boot app in Terminal 2 to stop the worker. Then tear down the cluster:

```
cd demos/01-cadence && docker compose down -v
```

Closing the terminal does **not** stop the containers — `docker compose up` ran them in the foreground, but the Cassandra, Cadence server, and Cadence web containers must be stopped explicitly. The `-v` also drops the Cassandra volume so the next run starts from a clean domain. Confirm with `docker ps` that nothing is left running.

## API

| Method | Path | Purpose |
|---|---|---|
| `POST` | `/start` | Start a workflow with the given input. Returns `{ workflowId }`. |
| `GET` | `/status/{id}` | If completed, returns the `DiagnosticsOutput`. Otherwise reports the running/timeout state. The real UI is Cadence Web. |
| `POST` | `/terminate/{id}` | Force-terminate the workflow. |
| `POST` | `/cancel/{id}` | Request graceful cancellation. (Cadence has no first-class pause/resume.) |

## Tests

```
./mvnw test
```

226 tests covering:
- `Severity` Jackson serialization (6)
- `MockAnalysisGenerator` invariants — repeated tests on health range and anomaly sampling (201)
- `PrioritizeIssuesActivity` severity thresholds with boundary cases (6)
- `NotifyBridgeActivity` canonical message format (2)
- `DiagnosticsWorkflow` end-to-end via `TestWorkflowEnvironment` — the 60-s timer is skipped by the virtual clock, so the suite runs in seconds (11)

## Comparison to the (planned) sister Temporal demo

A Temporal Java version of this same scenario lives (will live) at `../02-temporal/`. The compelling on-stage diff:

- `workflow/DiagnosticsWorkflowImpl.java` — body is byte-for-byte identical. The only differences:
  - `com.uber.cadence.*` vs `io.temporal.*` imports
  - `new ActivityOptions.Builder()` vs `ActivityOptions.newBuilder()`
  - An extra `.setScheduleToCloseTimeout(...)` (Cadence requires it)
- `config/CadenceConfig.java` (this demo) — does not exist in the Temporal demo. The Temporal Spring Boot starter handles the equivalent wiring.

## Talking points on stage

- *Cadence is from 2017; Temporal's Spring Boot starter is from 2022. One config file is the five years of ergonomics work.*
- *Domain* (Cadence) ↔ *namespace* (Temporal). *Task list* (Cadence) ↔ *task queue* (Temporal). The renaming happened at the fork.
- *The kill-and-resume payoff is identical — the* durable execution *primitive is what both inherit from the same lineage.*

## Cadence Java 3.13.0 — quirks worth noting

- No `@WorkflowInterface` / `@ActivityInterface` annotations exist; only the method-level `@WorkflowMethod` / `@ActivityMethod` are needed.
- `WorkflowStub.getResult` takes `(long timeout, TimeUnit unit, Class<T> type)` — not `Duration`.
- `WorkflowStub.terminate(...)` does not exist on the typed stub. The controller calls `client.getService().TerminateWorkflowExecution(...)` directly.
- `DiagnosticsWorkflowImpl` is NOT a Spring `@Component`. Cadence instantiates workflow impls itself per execution; if Spring tries to instantiate it on the main thread, the field-initializer `Workflow.newActivityStub(...)` calls crash with "Called from non workflow or workflow callback thread."
- Java 17+ requires `--add-opens java.base/java.lang=ALL-UNNAMED` for the Cadence client's reflection on `Throwable.cause`. Already wired into both `spring-boot:run` and the test runner in `pom.xml`.

## Pin / verify before the talk

- `cassandra:4.1`, `ubercadence/server:v1.2.18-auto-setup`, `ubercadence/web:v4.0.15`: pre-pull on the talk laptop; never `:latest`.
- `cadence-client` `3.13.0` pinned in `pom.xml`.
- Run `./mvnw dependency:go-offline` so Spring Boot startup doesn't pull from Maven Central on stage.
- Do a full end-to-end dry run including the kill-and-resume sequence on the talk laptop. (See the choreography section above.)
