# Temporal (Java) demo — design

**Status:** approved design, ready for implementation plan
**Author:** Marc Duiker
**Date:** 2026-05-16
**Target:** `demos/02-temporal/` (replaces the previously-planned Python implementation at `demos/01-temporal/`)
**Canonical scenario:** [`demos/shared/scenario.md`](../../../demos/shared/scenario.md) (mirrored verbatim from `plan.md` §4)
**Canonical reference implementation:** [`demos/04-dapr/`](../../../demos/04-dapr/) (.NET 10 + Aspire — see `plan.md` §5.4)

---

## 1. Goal

Implement the USS Enterprise diagnostics workflow in Temporal Java as one of the five framework demos for the "Durable Execution Battle" talk. The implementation must:

- Match the **canonical shared scenario** (§4 of `plan.md`) on activity *behavior*, severity thresholds, rigging strategy, and the kill-and-resume choreography.
- Hold the **cross-demo invariants** from `plan.md` §5.0 constant on the wire — subsystem string ids (`warp_core`, `shields`, `life_support`, `sensors`, `weapons`), severity wire values (`green`, `yellow_alert`, `red_alert`), health range `30..65`, bridge alert message format, and workflow id `enterprise-ncc-1701`.
- Be **idiomatic Java**, not a C# transliteration of the .NET reference. The .NET implementation at `demos/04-dapr/` is the **scenario reference**, NOT a style guide. Do not blindly copy `PascalCase` activity/class names, `Task.WhenAll` shapes, .NET-style file layout, or .NET DI patterns into Java. Use Maven layout, Java `record` types, Spring Boot conventions, `Promise.allOf`, `Workflow.newActivityStub`, `camelCase` methods, and the Temporal Spring Boot starter's auto-discovery — these are what an experienced Java/Temporal engineer would write from scratch.
- Use **idiomatic Java naming** throughout: packages (`io.diagrid.enterprisediagnostics.workflow`), class names (`DiagnosticsWorkflow` + `DiagnosticsWorkflowImpl` split — a Java convention, not a .NET one), method names (`runDiagnostics`, `execute` — `camelCase`), record component names (`shipName`, not `ShipName`), enum constants in `SCREAMING_SNAKE_CASE` (`RED_ALERT`) with Jackson-serialized snake_case wire values (`red_alert`).
- Run from a fresh laptop in under 2 minutes (`temporal server start-dev` + `./mvnw spring-boot:run`).

## 2. Locked decisions

| Choice | Decision | Why |
|---|---|---|
| **Language / runtime** | Java 21 LTS | Current LTS; records + pattern matching + sealed types fit the model classes cleanly |
| **Build tool** | Maven | Most-recognized in enterprise Java; cleanest pom.xml read on a slide |
| **HTTP framework** | Spring Boot 3.x | First-class Temporal integration via `temporal-spring-boot-starter-alpha`; familiar to the audience |
| **Temporal SDK** | `io.temporal:temporal-sdk` + `io.temporal:temporal-spring-boot-starter-alpha` | Starter auto-discovers workflow/activity beans and creates `WorkerFactory` + `Worker` per task queue |
| **Temporal server** | `temporal` CLI dev server (`temporal server start-dev`) | Single binary, in-memory, Temporal Web bundled on `:8233`; matches the Python/TypeScript demos' "no infra" feel |
| **Project layout** | Single Maven module under `demos/02-temporal/` | Aspire's `AppHost` multi-project shape doesn't translate to Temporal — collapsing to one module is the honest Java idiom and keeps the on-stage file count low |
| **Task queue** | `diagnostics-tq` | Single queue; worker + workflow + activities all on it |
| **Plan.md positioning** | Final folder layout: `01-cadence/`, `02-temporal/` (this spec, Java), `03-conductor/`, `04-dapr/`, `05-restate/`. The old `01-temporal/` Python placeholder is removed; Cadence shifts up from `02-cadence/` to `01-cadence/`; Conductor / Dapr / Restate keep their existing numbers. Talk timeline, `.demo` file names, and §5 subsection order in `plan.md` realign to this — Cadence first, Temporal-Java second. | Folder number = talk sequence; lineage-first narration (Cadence → Temporal-the-fork) reads naturally |

## 3. Project layout

```
demos/02-temporal/
├── README.md                          # run instructions + talk choreography
├── pom.xml                            # Spring Boot parent + Temporal SDK + starter
├── mvnw, mvnw.cmd, .mvn/              # Maven wrapper
├── enterprise-diagnostics.http        # POST /start, GET /status/{id}, etc. — IntelliJ/VS Code REST Client
└── src/main/
    ├── java/io/diagrid/enterprisediagnostics/
    │   ├── EnterpriseDiagnosticsApplication.java    # @SpringBootApplication
    │   ├── model/
    │   │   ├── DiagnosticsInput.java                # record
    │   │   ├── DiagnosticsOutput.java               # record, bridgeNotification is nullable
    │   │   ├── ScanInput.java                       # record { shipName, subsystem }
    │   │   ├── ScanResult.java                      # record { subsystem, health, anomalies[] }
    │   │   ├── PrioritizeInput.java                 # record { shipName, scans[] }
    │   │   ├── Priority.java                        # record { severity, summary }
    │   │   ├── Severity.java                        # enum { GREEN, YELLOW_ALERT, RED_ALERT } + JSON serialization to snake_case
    │   │   ├── BridgeNotificationInput.java         # record { shipName, engineerName, priority }
    │   │   └── BridgeNotificationResult.java        # record { message }
    │   ├── workflow/
    │   │   ├── DiagnosticsWorkflow.java             # @WorkflowInterface
    │   │   └── DiagnosticsWorkflowImpl.java         # @WorkflowImpl(taskQueues = "diagnostics-tq")
    │   ├── activity/
    │   │   ├── ScanWarpCoreActivity.java            # @ActivityInterface (single execute(ScanInput): ScanResult method)
    │   │   ├── ScanWarpCoreActivityImpl.java        # @Component @ActivityImpl(taskQueues = "diagnostics-tq")
    │   │   ├── ScanShieldsActivity.java             #   …same pattern…
    │   │   ├── ScanShieldsActivityImpl.java
    │   │   ├── ScanLifeSupportActivity.java
    │   │   ├── ScanLifeSupportActivityImpl.java
    │   │   ├── ScanSensorsActivity.java
    │   │   ├── ScanSensorsActivityImpl.java
    │   │   ├── ScanWeaponsActivity.java
    │   │   ├── ScanWeaponsActivityImpl.java
    │   │   ├── PrioritizeIssuesActivity.java
    │   │   ├── PrioritizeIssuesActivityImpl.java
    │   │   ├── NotifyBridgeActivity.java
    │   │   ├── NotifyBridgeActivityImpl.java
    │   │   └── MockAnalysisGenerator.java           # shared scan-result generator (mirrors .NET MockAnalysisGenerator)
    │   └── api/
    │       └── DiagnosticsController.java           # @RestController: POST /start, GET /status/{id}, terminate, suspend/resume
    └── resources/
        └── application.yml                          # spring.temporal.{namespace, target}, workers[].taskQueue=diagnostics-tq
```

**Why one `@ActivityInterface` per scan subsystem (not a single `ScanActivities` interface with 5 methods):**
The .NET reference uses one class per subsystem so each activity type is independently retryable, traceable, and identifiable in the worker UI. Mirroring that in Java keeps the activity names visible in Temporal Web exactly as they are in the Diagrid Dashboard (`ScanWarpCoreActivity`, `ScanShieldsActivity`, …), which is the on-stage payoff.

## 4. Model shapes (Java records)

All records are `record` types, Jackson-serializable, with field names matching the .NET reference (`camelCase` JSON wire format).

```java
public record DiagnosticsInput(
    String id,                // also the Temporal workflow id
    String shipName,
    String diagnosticsDate,
    String engineerName) {}

public record DiagnosticsOutput(
    String shipName,
    String diagnosticsDate,
    String engineerName,
    Priority priority,
    BridgeNotificationResult bridgeNotification) {}   // nullable — null = green path

public record ScanInput(String shipName, String subsystem) {}
public record ScanResult(String subsystem, int health, List<String> anomalies) {}

public record PrioritizeInput(String shipName, List<ScanResult> scans) {}
public record Priority(Severity severity, String summary) {}

public enum Severity {
    GREEN, YELLOW_ALERT, RED_ALERT;
    // Jackson serializes/deserializes as "green" / "yellow_alert" / "red_alert"
}

public record BridgeNotificationInput(String shipName, String engineerName, Priority priority) {}
public record BridgeNotificationResult(String message) {}
```

## 5. Workflow shape

```java
@WorkflowInterface
public interface DiagnosticsWorkflow {
    @WorkflowMethod
    DiagnosticsOutput runDiagnostics(DiagnosticsInput input);
}

@WorkflowImpl(taskQueues = "diagnostics-tq")
public class DiagnosticsWorkflowImpl implements DiagnosticsWorkflow {

    private static final ActivityOptions ACTIVITY_OPTIONS = ActivityOptions.newBuilder()
        .setStartToCloseTimeout(Duration.ofSeconds(30))
        .setRetryOptions(RetryOptions.newBuilder()
            .setInitialInterval(Duration.ofSeconds(3))
            .setBackoffCoefficient(1.5)
            .setMaximumAttempts(5)
            .build())
        .build();

    private final ScanWarpCoreActivity warpCore       = Workflow.newActivityStub(ScanWarpCoreActivity.class, ACTIVITY_OPTIONS);
    private final ScanShieldsActivity shields         = Workflow.newActivityStub(ScanShieldsActivity.class, ACTIVITY_OPTIONS);
    private final ScanLifeSupportActivity lifeSupport = Workflow.newActivityStub(ScanLifeSupportActivity.class, ACTIVITY_OPTIONS);
    private final ScanSensorsActivity sensors         = Workflow.newActivityStub(ScanSensorsActivity.class, ACTIVITY_OPTIONS);
    private final ScanWeaponsActivity weapons         = Workflow.newActivityStub(ScanWeaponsActivity.class, ACTIVITY_OPTIONS);
    private final PrioritizeIssuesActivity prioritize = Workflow.newActivityStub(PrioritizeIssuesActivity.class, ACTIVITY_OPTIONS);
    private final NotifyBridgeActivity notify         = Workflow.newActivityStub(NotifyBridgeActivity.class, ACTIVITY_OPTIONS);

    @Override
    public DiagnosticsOutput runDiagnostics(DiagnosticsInput input) {
        var logger = Workflow.getLogger(DiagnosticsWorkflowImpl.class);
        logger.info("Starting diagnostics for {}", input.shipName());

        // Fan-out — 5 parallel scans
        Promise<ScanResult> pWarp        = Async.function(warpCore::execute,    new ScanInput(input.shipName(), "warp_core"));
        Promise<ScanResult> pShields     = Async.function(shields::execute,     new ScanInput(input.shipName(), "shields"));
        Promise<ScanResult> pLifeSupport = Async.function(lifeSupport::execute, new ScanInput(input.shipName(), "life_support"));
        Promise<ScanResult> pSensors     = Async.function(sensors::execute,     new ScanInput(input.shipName(), "sensors"));
        Promise<ScanResult> pWeapons     = Async.function(weapons::execute,     new ScanInput(input.shipName(), "weapons"));

        Promise.allOf(pWarp, pShields, pLifeSupport, pSensors, pWeapons).get();

        List<ScanResult> scans = List.of(
            pWarp.get(), pShields.get(), pLifeSupport.get(), pSensors.get(), pWeapons.get());

        // ⚡ KILL POINT — durable timer ⚡
        logger.info("Engineering review pending — sleeping 60s");
        Workflow.sleep(Duration.ofSeconds(60));

        Priority priority = prioritize.execute(new PrioritizeInput(input.shipName(), scans));

        BridgeNotificationResult bridgeResult = null;
        if (priority.severity() == Severity.YELLOW_ALERT || priority.severity() == Severity.RED_ALERT) {
            bridgeResult = notify.execute(
                new BridgeNotificationInput(input.shipName(), input.engineerName(), priority));
        }

        return new DiagnosticsOutput(
            input.shipName(),
            input.diagnosticsDate(),
            input.engineerName(),
            priority,
            bridgeResult);
    }
}
```

**Determinism notes:**

- `Workflow.getLogger(...)` is the replay-safe logger; do not use SLF4J directly inside the workflow body.
- All side-effecting / non-deterministic work lives in activities. No `new Random()`, no `LocalDateTime.now()`, no `Thread.sleep` inside the workflow.
- Scan dispatch is via `Async.function(stub::method, input)`; this returns immediately with a `Promise`. Calling `.get()` *before* `Promise.allOf(...).get()` would serialize the fan-out — the design above intentionally awaits all together.

## 6. Activity behavior

### 6.1 Scan activities

Each `Scan{Subsystem}ActivityImpl` is a `@Component` (Spring bean) so the Temporal Spring Boot starter auto-discovers and registers it on the `diagnostics-tq` task queue.

```java
@ActivityInterface
public interface ScanWarpCoreActivity {
    ScanResult execute(ScanInput input);
}

@Component
@ActivityImpl(taskQueues = "diagnostics-tq")
public class ScanWarpCoreActivityImpl implements ScanWarpCoreActivity {

    private static final List<String> CANDIDATES = List.of(
        "plasma conduit micro-fracture (deck 36)",
        "warp coil phase variance 0.04%",
        "dilithium articulation frame torque drift",
        "matter/antimatter injector hesitation",
        "magnetic constriction segment 7 thermal creep");

    @Override
    public ScanResult execute(ScanInput input) {
        return MockAnalysisGenerator.generate(input.subsystem(), CANDIDATES);
    }
}
```

The four other scan activities have the same shape with a per-subsystem `CANDIDATES` list. **No `Thread.sleep`** — the activity returns immediately. The 60-second kill window comes from the workflow's `Workflow.sleep(60s)` between fan-in and prioritization, not from artificial activity latency.

### 6.2 `MockAnalysisGenerator`

Direct Java port of the .NET helper. Single static method, biased-low health range so the demo reliably hits `RED_ALERT`:

```java
public final class MockAnalysisGenerator {
    private static final SecureRandom RANDOM = new SecureRandom();

    public static ScanResult generate(String subsystem, List<String> candidateAnomalies) {
        int health = RANDOM.nextInt(30, 66);                       // 30..65 inclusive — biased low
        int maxAnomalies = (health >= 60) ? 1 : (health >= 45) ? 2 : 3;
        int anomalyCount = RANDOM.nextInt(1, maxAnomalies + 1);

        List<String> shuffled = new ArrayList<>(candidateAnomalies);
        Collections.shuffle(shuffled, RANDOM);
        List<String> picked = shuffled.subList(0, anomalyCount);

        return new ScanResult(subsystem, health, List.copyOf(picked));
    }

    private MockAnalysisGenerator() {}
}
```

This is called from the **activity**, not the workflow, so the randomness is safe.

### 6.3 `PrioritizeIssuesActivity`

Deterministic. Severity from `min(health)` across the scans:

```java
@Override
public Priority execute(PrioritizeInput input) {
    ScanResult worst = input.scans().stream()
        .min(Comparator.comparingInt(ScanResult::health))
        .orElseThrow();

    Severity severity =
        worst.health() < 50 ? Severity.RED_ALERT :
        worst.health() < 75 ? Severity.YELLOW_ALERT :
                              Severity.GREEN;

    long belowThreshold = input.scans().stream()
        .filter(s -> s.health() < 75)
        .count();

    String summary = "%s critical at %d%% health; %d subsystem(s) below threshold"
        .formatted(worst.subsystem(), worst.health(), belowThreshold);

    return new Priority(severity, summary);
}
```

### 6.4 `NotifyBridgeActivity`

Logs to stdout (the worker's process). No external service.

```java
@Override
public BridgeNotificationResult execute(BridgeNotificationInput input) {
    String message = "🚨 BRIDGE ALERT — USS %s — %s: %s (engineer: %s)".formatted(
        input.shipName(),
        input.priority().severity().name(),
        input.priority().summary(),
        input.engineerName());
    log.warn(message);   // SLF4J at WARN so it stands out in stdout
    return new BridgeNotificationResult(message);
}
```

`log` here is a normal SLF4J logger (this is an activity, not a workflow — SLF4J is fine).

## 7. HTTP API

A single `@RestController` mirroring the .NET endpoints. Spring Boot's embedded Tomcat hosts it on port 8080.

| Method | Path | Action |
|---|---|---|
| `POST` | `/start` | `workflowClient.newWorkflowStub(DiagnosticsWorkflow.class, WorkflowOptions.newBuilder().setWorkflowId(input.id()).setTaskQueue("diagnostics-tq").build())` then `WorkflowClient.start(stub::runDiagnostics, input)`. Returns `{ workflowId }`. |
| `GET` | `/status/{id}` | `workflowClient.newUntypedWorkflowStub(id).describe()` for state; if completed, read result via typed stub. Returns `{ status, output? }`. |
| `POST` | `/terminate/{id}` | `workflowClient.newUntypedWorkflowStub(id).terminate("manual termination", reason)`. |
| `POST` | `/cancel/{id}` | Same stub `.cancel()`. (Note: Temporal Java's analog to Dapr's `Suspend`/`Resume` is `cancel`/no-resume; document this in the README — Temporal does not have first-class pause/resume the way Dapr does.) |

The `WorkflowClient` is auto-wired by the Temporal Spring Boot starter from `application.yml` configuration.

**Sample `enterprise-diagnostics.http`:**
```http
### Start diagnostics
POST http://localhost:8080/start
Content-Type: application/json

{
  "id": "enterprise-ncc-1701",
  "shipName": "USS Enterprise NCC-1701-D",
  "diagnosticsDate": "2370-04-08",
  "engineerName": "Geordi La Forge"
}

### Status
GET http://localhost:8080/status/enterprise-ncc-1701
```

## 8. Runtime — how to run on stage

```bash
# Terminal 1 — Temporal server (single binary; Temporal Web on http://localhost:8233)
temporal server start-dev

# Terminal 2 — Spring Boot app (worker + REST API in one process)
cd demos/02-temporal && ./mvnw spring-boot:run

# Terminal 3 — start a workflow
curl -X POST http://localhost:8080/start \
  -H 'Content-Type: application/json' \
  -d '{"id":"enterprise-ncc-1701","shipName":"USS Enterprise NCC-1701-D","diagnosticsDate":"2370-04-08","engineerName":"Geordi La Forge"}'
```

Audience watches **Temporal Web** at `http://localhost:8233` — event history shows:

1. `WorkflowExecutionStarted`
2. 5 `ActivityTaskScheduled` + 5 `ActivityTaskStarted` + 5 `ActivityTaskCompleted` (the fan-out, near-instant)
3. `TimerStarted` (60s)
4. *(Marc Ctrl+C the Spring Boot app at ~T+20s into the timer)*
5. *(Marc restarts `./mvnw spring-boot:run`)*
6. `TimerFired`
7. `ActivityTaskScheduled` / `…Completed` for `PrioritizeIssuesActivity`
8. `ActivityTaskScheduled` / `…Completed` for `NotifyBridgeActivity` (only because severity = `red_alert`)
9. `WorkflowExecutionCompleted`

The Spring Boot app hosts both the worker and the REST API in one process, so killing the app is killing the worker — exactly the demo beat we want (the workflow's pending timer survives in the Temporal server, the worker restarts, and the timer durably wakes).

## 9. `pom.xml` highlights

```xml
<parent>
  <groupId>org.springframework.boot</groupId>
  <artifactId>spring-boot-starter-parent</artifactId>
  <version>3.4.x</version>            <!-- pin actual version pre-talk -->
</parent>

<properties>
  <java.version>21</java.version>
  <temporal.version>1.30.x</temporal.version>   <!-- pin actual version pre-talk -->
</properties>

<dependencies>
  <dependency>
    <groupId>org.springframework.boot</groupId>
    <artifactId>spring-boot-starter-web</artifactId>
  </dependency>
  <dependency>
    <groupId>io.temporal</groupId>
    <artifactId>temporal-sdk</artifactId>
    <version>${temporal.version}</version>
  </dependency>
  <dependency>
    <groupId>io.temporal</groupId>
    <artifactId>temporal-spring-boot-starter-alpha</artifactId>
    <version>${temporal.version}</version>
  </dependency>
</dependencies>
```

Spring Boot starter is `-alpha` but production-quality (Temporal team's own naming). Mention this honestly in the README; do not let an audience question about "alpha" derail the demo.

## 10. `application.yml`

```yaml
spring:
  temporal:
    namespace: default
    connection:
      target: 127.0.0.1:7233
    workers:
      - task-queue: diagnostics-tq
        name: diagnostics-worker

server:
  port: 8080
```

## 11. plan.md updates (applied)

Final folder layout: `01-cadence/`, `02-temporal/` (Java, this spec), `03-conductor/`, `04-dapr/`, `05-restate/`. Cadence shifts up from `02-cadence/` to `01-cadence/`; the old `01-temporal/` Python entry is deleted entirely; Conductor / Dapr / Restate keep their existing numbers and contents. Talk timeline, `.demo` filenames, and `§5` subsection ordering all realign so Cadence is first and Temporal-Java is second.

These edits to `plan.md` have already been applied alongside this spec; this table is the audit trail.

| Section in `plan.md` | Change |
|---|---|
| Locked decisions table | `Temporal **Python**` → `Temporal **Java**` in the "Demo languages" row |
| §1 timeline (demo block, lines 29–34) | Reorder demo segments to **Cadence (Go) 4 min → Temporal (Java) 5 min → Conductor (Python) 5 min → Dapr (.NET) 6 min → Restate (TS) 5 min**. Recompute start times: 18:00 Cadence, 22:00 Temporal, 27:00 Conductor, 32:00 Dapr, 38:00 Restate. Cadence's bullet swaps from "note SDK gap" to "the ancestor — sets up the lineage" (or similar); Temporal's bullet becomes the workflow-as-code default. |
| §2 repository layout | Delete the `01-temporal/` (Python) block. Rename `02-cadence/` → `01-cadence/`. Insert a new `02-temporal/` (Java) block with Maven layout (see §3 of this spec for the file tree). `03-conductor/`, `04-dapr/`, `05-restate/` blocks stay as-is. |
| §2 `.demo` file list | Rename: `01-temporal.demo` is removed; `02-cadence.demo` → `01-cadence.demo`; add `02-temporal.demo` (Java). `03-conductor.demo`, `04-dapr.demo`, `05-restate.demo` unchanged. |
| §5 demo subsections | Reorder so §5.1 = Cadence, §5.2 = Temporal (Java), §5.3 = Conductor, §5.4 = Dapr, §5.5 = Restate. The Cadence subsection's file paths shift to `demos/01-cadence/...`. |
| §5.2 Temporal subsection (was §5.1, Python) | Rewrite for Java: file `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflowImpl.java`; workflow shape using `@WorkflowInterface` + `Async.function` + `Promise.allOf`; sleep `Workflow.sleep(Duration.ofSeconds(60))`; runtime `temporal server start-dev` + `./mvnw spring-boot:run`; visual `localhost:8233`. |
| §5.2 idiomatic tip | Replace the Python-sandbox tip with: "Workflow code is a sandboxed deterministic class — no `Random`, no `LocalDateTime.now()`, no `Thread.sleep`. Activities are normal Spring beans where the side-effects live." |
| §8 authoring order | Update the Temporal-demo line to swap `Python` → `Java` and bump the time estimate if needed. The order ("Dapr → Temporal → Restate → Conductor → Cadence") stays the same — authoring order is independent of folder numbering. |
| §7 pre-talk checklist | No new `docker pull` (Temporal dev server is a single CLI binary). Add: `brew install temporal` on the talk laptop; `./mvnw dependency:go-offline` in `demos/02-temporal/` to warm the Maven cache. |

## 12. Mapping — .NET vs Java cheat sheet

For on-stage code comparison slides (`02-languages.md` may want this):

| .NET (Dapr) | Java (Temporal) |
|---|---|
| `Workflow<TIn, TOut>` base class | `@WorkflowInterface` + `@WorkflowMethod` |
| `WorkflowActivity<TIn, TOut>` base class | `@ActivityInterface` + impl class |
| `context.CallActivityAsync<T>(name, input, retryPolicy)` | `Workflow.newActivityStub(StubInterface.class, options)` then `stub.method(input)` |
| `Task.WhenAll(...)` on Task objects | `Promise.allOf(...).get()` on Promise objects |
| `context.CreateTimer(TimeSpan.FromSeconds(60))` | `Workflow.sleep(Duration.ofSeconds(60))` |
| `context.CreateReplaySafeLogger<T>()` | `Workflow.getLogger(T.class)` |
| `WorkflowRetryPolicy(maxAttempts, initialInterval, backoff)` | `RetryOptions.newBuilder().setMaximumAttempts(...).setInitialInterval(...).setBackoffCoefficient(...)` |
| `DaprWorkflowClient.ScheduleNewWorkflowAsync(name, instanceId, input)` | `WorkflowClient.start(stub::method, input)` (workflow id set in `WorkflowOptions` at stub creation) |
| `DaprWorkflowClient.GetWorkflowStateAsync(id)` | `workflowClient.newUntypedWorkflowStub(id).describe()` |

## 13. Out of scope (first pass)

- **Workflow versioning.** Temporal Java has `Workflow.getVersion(changeId, minSupported, maxSupported)` but the canonical scenario doesn't require it. The .NET Dapr demo's source-generator versioning pattern is its own talking point; the Java demo doesn't need a parallel.
- **Diagrid Dashboard analog.** Temporal Web at `:8233` is the UI. No second dashboard needed.
- **Docker / docker-compose.** The CLI dev server is enough for stage. A compose file can land later if anyone asks for a "production-shaped" version.
- **Integration tests.** Temporal has a great `TestWorkflowEnvironment`; useful, but not on the critical path for the talk. Add only if a 5-minute Q&A question warrants it.
- **Persistent storage.** Dev server is in-memory. Restarting the Temporal CLI clears history. That's fine for stage; the demo's whole "durable" payoff is *within one server lifetime*, surviving worker restarts.
- **Authentication, metrics, tracing.** Stage demo only.

## 14. Risks

| Risk | Mitigation |
|---|---|
| `temporal-spring-boot-starter-alpha` API churn before talk day | Pin exact version in `pom.xml`; verify the demo end-to-end on the talk laptop a week before |
| Spring Boot startup time visible on stage (~3-5s) | Acceptable — runs once at start of demo. Pre-warm by starting it during the preceding dimension slide if it feels slow on the day |
| Audience asks "why Spring Boot instead of plain `main()`" | Pre-bake the answer: "Spring Boot's Temporal starter auto-discovers workflows/activities — same shape as the .NET Aspire dependency injection in the Dapr demo. Plain `main()` would mean explicitly wiring `WorkerFactory` + `Worker` + `WorkflowClient`; the demo would lose its idiomatic-Java feel." |
| `Workflow.sleep(Duration.ofSeconds(60))` vs `Workflow.newTimer(Duration.ofSeconds(60)).get()` — wrong call style | Use `Workflow.sleep` — it's the more idiomatic surface for blocking the workflow thread, and reads cleaner on stage. `newTimer` returns a `Promise` which is useful when you need to race against a signal, which we don't here |
| Java records + Jackson + Temporal data converter compatibility | Temporal Java uses Jackson by default; records have been first-class since Jackson 2.12. Verify with a smoke test before the talk; fall back to classic POJOs with `@JsonCreator` if anything breaks |
| Severity enum JSON serialization mismatches the .NET strings (`red_alert` vs `RED_ALERT`) | Use `@JsonProperty("red_alert")` per enum constant, or a custom `@JsonSerialize(using = ...)`. Lock in a unit test that round-trips against the literal JSON the .NET service emits |

## 15. Verification checklist (before declaring the demo done)

- [ ] `temporal server start-dev` runs and exposes `:8233` (web) and `:7233` (gRPC).
- [ ] `./mvnw spring-boot:run` starts in <10s and registers a worker on `diagnostics-tq` (visible in Temporal Web → Task Queues).
- [ ] `POST /start` returns `200 OK` with `{ "workflowId": "enterprise-ncc-1701" }`.
- [ ] In Temporal Web, the 5 scan activities complete near-instantly (no artificial delay), then a `TimerStarted` event appears.
- [ ] During the 60s timer, killing the Spring Boot process and restarting it does not advance the workflow until the timer fires; after it fires, prioritization runs and `NotifyBridgeActivity` is invoked.
- [ ] The `🚨 BRIDGE ALERT` line appears in the Spring Boot stdout at WARN level with the canonical message format.
- [ ] `GET /status/enterprise-ncc-1701` after completion returns a `DiagnosticsOutput` with non-null `bridgeNotification.message` (because severity reliably hits `red_alert`).
- [ ] Run the workflow 10× in a row — `bridgeNotification` is never null (rigging holds).
- [ ] Total wall-clock from `temporal server start-dev` cold start to completed workflow: under 90 seconds.
