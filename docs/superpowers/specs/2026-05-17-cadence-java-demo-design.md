# Cadence (Java) demo — design

**Status:** approved design, ready for implementation plan
**Author:** Marc Duiker
**Date:** 2026-05-17
**Target:** `demos/01-cadence/`
**Canonical scenario:** [`demos/shared/scenario.md`](../../../demos/shared/scenario.md) (mirrored verbatim from `plan.md` §4)
**Canonical reference implementation:** [`demos/04-dapr/`](../../../demos/04-dapr/) (.NET 10 + Aspire — see `plan.md` §5.4)
**Sister spec:** [`2026-05-16-temporal-java-demo-design.md`](./2026-05-16-temporal-java-demo-design.md) — the Temporal Java demo this one is the lineage twin of. Structural deltas vs that spec are the *whole point* of this design; §3 and §11 enumerate them.

---

## 1. Goal

Implement the USS Enterprise diagnostics workflow in **Cadence Java** as one of the five framework demos for the "Durable Execution Battle" talk. The implementation must:

- Match the **canonical shared scenario** (§4 of `plan.md`) on activity *behavior*, severity thresholds, rigging strategy, and the kill-and-resume choreography.
- Hold the **cross-demo invariants** from `plan.md` §5.0 constant on the wire — subsystem string ids (`warp_core`, `shields`, `life_support`, `sensors`, `weapons`), severity wire values (`green`, `yellow_alert`, `red_alert`), health range `30..65`, bridge alert message format, and workflow id `enterprise-ncc-1701`.
- Be **idiomatic Cadence Java** — same SDK *surface* as Temporal (Cadence is the ancestor; Temporal forked it), but using Cadence's package names, terminology (**domain** instead of namespace, **task list** instead of task queue), and the **manual `@Bean` wiring** that Cadence requires because there is no first-party Spring Boot starter.
- Run from a fresh laptop in under 3 minutes (`docker compose up` for Cassandra + Cadence server + Cadence Web, then `./mvnw spring-boot:run`).
- **Be the lineage demo.** When the audience watches this and then watches the Temporal demo immediately after, the on-stage payoff is: "the workflow code is almost identical — only the imports and one config class changed; that's *exactly* what 'fork' means here."

## 2. Locked decisions

| Choice | Decision | Why |
|---|---|---|
| **Language / runtime** | Java 21 LTS | Same as Temporal demo — keeps the side-by-side comparison clean (same record syntax, same pattern matching, same Jackson version) |
| **Build tool** | Maven | Mirrors Temporal demo's `pom.xml`; cleanest on-stage diff |
| **HTTP framework** | Spring Boot 3.x | Mirrors Temporal demo's Spring Boot; the *only* visible structural difference is the wiring class (auto-starter vs manual `@Bean`s), which is the talking point |
| **Cadence SDK** | `com.uber.cadence:cadence-client` (latest 3.x; pin pre-talk) | Uber's first-party Java client. There is **no** official `cadence-java-spring-boot-starter`; community starters exist but are not used here — manual wiring is the honest, documentable path |
| **Wiring approach** | **Manual `@Bean`s** in `CadenceConfig` for `IWorkflowService`, `WorkflowClient`, `WorkerFactory`, and one `Worker` per task list | Surfaces the ergonomics gap vs Temporal's starter; the diff between `CadenceConfig.java` (~40 lines) and the Temporal demo's *missing* config class IS the lineage point |
| **Cadence server** | `ubercadence/server` (pin tag; see §9) + Cassandra 4 + `ubercadence/web` via `docker-compose.yml` | No single-binary dev option exists for Cadence; this is the honest cost-of-entry vs Temporal's `start-dev` |
| **Domain** | `default` domain, **auto-registered on app startup** via a `@PostConstruct` hook on `CadenceConfig` (idempotent — swallow "DomainAlreadyExists") | Avoids a separate `cadence-cli` step on stage; one-binary developer experience inside the Maven module |
| **Task list** | `diagnostics-tl` | Cadence's term for what Temporal calls a task queue. Naming the constant `TASK_LIST` keeps the terminology audit-able on slides |
| **Project layout** | Single Maven module under `demos/01-cadence/` | Same shape as Temporal demo; the *only* structural addition is the `config/` package |
| **plan.md positioning** | `01-cadence/` (this spec) → `02-temporal/` (sister spec). Already applied in `plan.md` §5.1 and §5.2; this spec is the implementation contract | Folder number = talk sequence; Cadence-first = lineage-first narration |

## 3. Project layout

```
demos/01-cadence/
├── README.md                          # run instructions + talk choreography
├── pom.xml                            # Spring Boot parent + Cadence client
├── mvnw, mvnw.cmd, .mvn/              # Maven wrapper
├── docker-compose.yml                 # Cassandra + Cadence server + Cadence Web
├── enterprise-diagnostics.http        # POST /start, GET /status/{id}, terminate — IntelliJ/VS Code REST Client
└── src/main/
    ├── java/io/diagrid/enterprisediagnostics/
    │   ├── EnterpriseDiagnosticsApplication.java    # @SpringBootApplication
    │   ├── config/
    │   │   └── CadenceConfig.java                   # @Configuration — manual @Beans for IWorkflowService, WorkflowClient, WorkerFactory, Worker; @PostConstruct registers the domain
    │   ├── model/
    │   │   ├── DiagnosticsInput.java                # record
    │   │   ├── DiagnosticsOutput.java               # record, bridgeNotification is nullable
    │   │   ├── ScanInput.java                       # record { shipName, subsystem }
    │   │   ├── ScanResult.java                      # record { subsystem, health, anomalies[] }
    │   │   ├── PrioritizeInput.java                 # record { shipName, scans[] }
    │   │   ├── Priority.java                        # record { severity, summary }
    │   │   ├── Severity.java                        # enum { GREEN, YELLOW_ALERT, RED_ALERT } + Jackson snake_case wire format
    │   │   ├── BridgeNotificationInput.java         # record { shipName, engineerName, priority }
    │   │   └── BridgeNotificationResult.java        # record { message }
    │   ├── workflow/
    │   │   ├── DiagnosticsWorkflow.java             # @WorkflowInterface
    │   │   └── DiagnosticsWorkflowImpl.java         # plain class — registered manually with the worker
    │   ├── activity/
    │   │   ├── ScanWarpCoreActivity.java            # @ActivityInterface (single execute(ScanInput): ScanResult method)
    │   │   ├── ScanWarpCoreActivityImpl.java        # @Component — bean, but manually registered with the worker
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
    │   │   └── MockAnalysisGenerator.java           # shared scan-result generator (port of the .NET helper; identical to Temporal demo's copy)
    │   └── api/
    │       └── DiagnosticsController.java           # @RestController: POST /start, GET /status/{id}, terminate
    └── resources/
        └── application.yml                          # cadence.target=127.0.0.1:7933, cadence.domain=default, cadence.taskList=diagnostics-tl
```

**Structural delta vs the Temporal demo:**

```
┌───────────────────────── deltas ─────────────────────────┐
│ + demos/01-cadence/src/main/java/.../config/             │
│ +     CadenceConfig.java          ~40 lines, @Beans      │
│                                                          │
│ + demos/01-cadence/docker-compose.yml                    │
│       (Temporal uses `temporal server start-dev` instead)│
└──────────────────────────────────────────────────────────┘
```

Everything else (model/, workflow/, activity/, api/, application.yml) is shape-identical to the Temporal demo. The on-stage diff between the two `DiagnosticsWorkflowImpl.java` files comes down to:

```diff
-import io.temporal.workflow.*;
-import io.temporal.activity.*;
+import com.uber.cadence.workflow.*;
+import com.uber.cadence.activity.*;
```

…plus the `@WorkflowImpl(taskQueues = "diagnostics-tq")` annotation on the Temporal side (auto-discovery hook for the starter) being **absent** on the Cadence side — Cadence's `DiagnosticsWorkflowImpl` is registered explicitly in `CadenceConfig.workerFactory()`. This is the entire visible difference for the audience.

## 4. Model shapes (Java records)

Identical to the Temporal demo. Same package, same record signatures, same Jackson `snake_case` wire format on `Severity`. **Do not duplicate the implementation — the records are copied verbatim** from `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/model/`. The on-stage payoff requires byte-for-byte identical model files so the audience can flip between the two source trees and see "yep, same shapes."

```java
public record DiagnosticsInput(
    String id,                // also the Cadence workflow id
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
import com.uber.cadence.activity.ActivityOptions;
import com.uber.cadence.workflow.Async;
import com.uber.cadence.workflow.Promise;
import com.uber.cadence.workflow.Workflow;
import com.uber.cadence.workflow.WorkflowInterface;
import com.uber.cadence.workflow.WorkflowMethod;

@WorkflowInterface
public interface DiagnosticsWorkflow {
    @WorkflowMethod
    DiagnosticsOutput runDiagnostics(DiagnosticsInput input);
}

public class DiagnosticsWorkflowImpl implements DiagnosticsWorkflow {

    private static final ActivityOptions ACTIVITY_OPTIONS = new ActivityOptions.Builder()
        .setScheduleToCloseTimeout(Duration.ofSeconds(60))
        .setStartToCloseTimeout(Duration.ofSeconds(30))
        .setRetryOptions(new RetryOptions.Builder()
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

**The Cadence/Temporal source-code diff for this file** (paste in `02-languages.md` slide):

```diff
-import io.temporal.activity.ActivityOptions;
-import io.temporal.workflow.*;
+import com.uber.cadence.activity.ActivityOptions;
+import com.uber.cadence.workflow.*;

-private static final ActivityOptions ACTIVITY_OPTIONS = ActivityOptions.newBuilder()
+private static final ActivityOptions ACTIVITY_OPTIONS = new ActivityOptions.Builder()
+    .setScheduleToCloseTimeout(Duration.ofSeconds(60))                  // Cadence: required
     .setStartToCloseTimeout(Duration.ofSeconds(30))
-    .setRetryOptions(RetryOptions.newBuilder()
+    .setRetryOptions(new RetryOptions.Builder()
         .setInitialInterval(Duration.ofSeconds(3))
         .setBackoffCoefficient(1.5)
         .setMaximumAttempts(5)
         .build())
     .build();

-@WorkflowImpl(taskQueues = "diagnostics-tq")                            // Temporal: starter auto-discovery
 public class DiagnosticsWorkflowImpl implements DiagnosticsWorkflow {  // Cadence: registered manually in CadenceConfig
```

Body of `runDiagnostics` is **byte-for-byte identical**.

**Determinism notes** (same as Temporal):

- `Workflow.getLogger(...)` is the replay-safe logger; do not use SLF4J directly inside the workflow body.
- All side-effecting / non-deterministic work lives in activities. No `new Random()`, no `LocalDateTime.now()`, no `Thread.sleep` inside the workflow.
- `Async.function(stub::method, input)` returns immediately with a `Promise`; `Promise.allOf(...).get()` is the fan-in.

## 6. Activity behavior

### 6.1 Scan activities

Each `Scan{Subsystem}ActivityImpl` is a `@Component` (Spring bean). Unlike Temporal's starter-driven auto-registration, **Cadence requires the worker to be told about each activity impl explicitly** in `CadenceConfig.workerFactory()` — see §10.

```java
import com.uber.cadence.activity.ActivityInterface;
import com.uber.cadence.activity.ActivityMethod;

@ActivityInterface
public interface ScanWarpCoreActivity {
    @ActivityMethod
    ScanResult execute(ScanInput input);
}

@Component
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

The four other scan activities have the same shape with a per-subsystem `CANDIDATES` list. **No `Thread.sleep`** — the activity returns immediately. The 60-second kill window comes from `Workflow.sleep(60s)` in the workflow body, not from artificial activity latency.

### 6.2 `MockAnalysisGenerator`

**Identical to the Temporal demo's copy** — same biased-low health range, same anomaly sampling. Suggested implementation: copy the file verbatim from `demos/02-temporal/.../activity/MockAnalysisGenerator.java`. Keeping the two copies identical keeps the on-stage source-code diff to imports-and-one-config-class.

### 6.3 `PrioritizeIssuesActivity`

Deterministic. Severity from `min(health)` across the scans. Same body as the Temporal demo's `PrioritizeIssuesActivityImpl`:

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

Same as Temporal:

```java
@Override
public BridgeNotificationResult execute(BridgeNotificationInput input) {
    String message = "🚨 BRIDGE ALERT — USS %s — %s: %s (engineer: %s)".formatted(
        input.shipName(),
        input.priority().severity().name(),
        input.priority().summary(),
        input.engineerName());
    log.warn(message);   // SLF4J at WARN
    return new BridgeNotificationResult(message);
}
```

`log` here is a normal SLF4J logger (this is an activity, not a workflow).

## 7. HTTP API

A single `@RestController` mirroring the Temporal demo's endpoints. Spring Boot's embedded Tomcat hosts it on port 8080.

| Method | Path | Action |
|---|---|---|
| `POST` | `/start` | Build `WorkflowOptions` with `setTaskList("diagnostics-tl")`, `setWorkflowId(input.id())`, `setExecutionStartToCloseTimeout(Duration.ofMinutes(10))`. Create stub via `workflowClient.newWorkflowStub(DiagnosticsWorkflow.class, options)`. Start with `WorkflowClient.start(stub::runDiagnostics, input)`. Returns `{ workflowId }`. |
| `GET` | `/status/{id}` | `workflowClient.newUntypedWorkflowStub(id, Optional.empty(), Optional.empty())` then describe via the underlying `IWorkflowService.DescribeWorkflowExecution(...)`. Returns `{ status, output? }`. |
| `POST` | `/terminate/{id}` | `workflowClient.newUntypedWorkflowStub(id, ...).terminate("manual", "manual termination")`. |
| `POST` | `/cancel/{id}` | Same stub `.cancel()`. (Cadence, like Temporal, has no first-class pause/resume — document this in README.) |

**Sample `enterprise-diagnostics.http`** (verbatim from Temporal demo — same wire format, same port):

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
# Terminal 1 — Cadence cluster (Cassandra + Cadence server + Cadence Web on http://localhost:8088)
cd demos/01-cadence && docker compose up

# Terminal 2 — Spring Boot app (worker + REST API in one process)
cd demos/01-cadence && ./mvnw spring-boot:run

# Terminal 3 — start a workflow
curl -X POST http://localhost:8080/start \
  -H 'Content-Type: application/json' \
  -d '{"id":"enterprise-ncc-1701","shipName":"USS Enterprise NCC-1701-D","diagnosticsDate":"2370-04-08","engineerName":"Geordi La Forge"}'
```

Audience watches **Cadence Web** at `http://localhost:8088` → `default` domain → workflow `enterprise-ncc-1701`. History shows:

1. `WorkflowExecutionStarted`
2. 5 × `ActivityTaskScheduled` + 5 × `ActivityTaskStarted` + 5 × `ActivityTaskCompleted` (fan-out, near-instant)
3. `TimerStarted` (60s)
4. *(Marc Ctrl+C the Spring Boot app at ~T+20s into the timer)*
5. *(Marc restarts `./mvnw spring-boot:run`)*
6. `TimerFired`
7. `ActivityTaskScheduled` / `…Completed` for `PrioritizeIssuesActivity`
8. `ActivityTaskScheduled` / `…Completed` for `NotifyBridgeActivity` (only because severity = `red_alert`)
9. `WorkflowExecutionCompleted`

The Spring Boot app hosts both the worker and the REST API in one process, so killing the app is killing the worker — same demo beat as Temporal. The Cadence server keeps the timer; the worker restarts; the timer durably wakes.

### Why a docker-compose cluster instead of a single binary

Cadence has no `start-dev` equivalent — it has always required Cassandra (or MySQL) as the persistence store, plus its own server, plus an optional web UI. The compose file makes this one command for the audience, but the operational footprint vs Temporal's single binary **is itself the talking point**: "the original architecture; Temporal's `start-dev` is the post-fork ergonomics improvement." Don't try to hide it.

## 9. `docker-compose.yml`

```yaml
services:
  cassandra:
    image: cassandra:4.1
    ports: ["9042:9042"]
    environment:
      CASSANDRA_LISTEN_ADDRESS: auto
    healthcheck:
      test: ["CMD", "cqlsh", "-e", "describe keyspaces"]
      interval: 15s
      timeout: 5s
      retries: 12

  cadence:
    image: ubercadence/server:1.2.13          # pin to a known-good tag pre-talk
    ports: ["7933:7933", "7934:7934", "7935:7935", "7939:7939"]
    environment:
      CASSANDRA_SEEDS: cassandra
      LOG_LEVEL: warn                          # quiet stdout — the audience reads code, not server logs
    depends_on:
      cassandra:
        condition: service_healthy

  cadence-web:
    image: ubercadence/web:4.1.4
    ports: ["8088:8088"]
    environment:
      CADENCE_GRPC_PEERS: cadence:7833
      CADENCE_GRPC_SERVICES_NAME: cadence-frontend
    depends_on:
      - cadence
```

> **Pre-talk verify**: tag `1.2.13` is illustrative — check `docker pull ubercadence/server:<tag>` for a current stable. Pin whatever you confirm works on the talk laptop into the compose file; never `:latest`.

## 10. `pom.xml` highlights

```xml
<parent>
  <groupId>org.springframework.boot</groupId>
  <artifactId>spring-boot-starter-parent</artifactId>
  <version>3.4.x</version>            <!-- pin actual version pre-talk; match Temporal demo -->
</parent>

<properties>
  <java.version>21</java.version>
  <cadence.version>3.13.x</cadence.version>   <!-- pin actual version pre-talk -->
</properties>

<dependencies>
  <dependency>
    <groupId>org.springframework.boot</groupId>
    <artifactId>spring-boot-starter-web</artifactId>
  </dependency>
  <dependency>
    <groupId>com.uber.cadence</groupId>
    <artifactId>cadence-client</artifactId>
    <version>${cadence.version}</version>
  </dependency>
</dependencies>
```

Notice the **one-dependency** delta from the Temporal `pom.xml`: Temporal needs both `temporal-sdk` *and* `temporal-spring-boot-starter-alpha`; Cadence needs only `cadence-client` because there is no first-party starter — the wiring lives in `CadenceConfig.java` instead.

## 11. `application.yml` + `CadenceConfig.java`

### `application.yml`

```yaml
cadence:
  service-name: cadence-frontend
  host: 127.0.0.1
  port: 7933
  domain: default
  task-list: diagnostics-tl

server:
  port: 8080
```

No `spring.cadence.*` namespace — Cadence has no first-party Boot integration, so the keys live under a flat `cadence:` root that `CadenceConfig` reads via `@ConfigurationProperties`.

### `CadenceConfig.java` — the structural delta

This is the *one file* that exists in the Cadence demo but **not** the Temporal demo. It's also the single file that's worth showing on stage twice — once as code, once as "and this is what Temporal's Spring Boot starter does for you in 0 lines."

```java
@Configuration
@EnableConfigurationProperties(CadenceProperties.class)
public class CadenceConfig {

    public static final String TASK_LIST = "diagnostics-tl";
    public static final String DOMAIN = "default";

    private final CadenceProperties props;

    public CadenceConfig(CadenceProperties props) {
        this.props = props;
    }

    @Bean(destroyMethod = "close")
    public IWorkflowService workflowService() {
        return new WorkflowServiceTChannel(
            ClientOptions.newBuilder()
                .setHost(props.host())
                .setPort(props.port())
                .setServiceName(props.serviceName())
                .build());
    }

    @Bean
    public WorkflowClient workflowClient(IWorkflowService service) {
        return WorkflowClient.newInstance(service,
            WorkflowClientOptions.newBuilder()
                .setDomain(DOMAIN)
                .build());
    }

    @Bean(destroyMethod = "shutdownNow")
    public WorkerFactory workerFactory(
            WorkflowClient client,
            DiagnosticsWorkflowImpl workflowImpl,             // <- plain class; not a bean by default
            List<ScanActivity> scanActivities,                // <- marker interface implemented by all 5 scan impls
            PrioritizeIssuesActivityImpl prioritize,
            NotifyBridgeActivityImpl notify) {

        WorkerFactory factory = WorkerFactory.newInstance(client);
        Worker worker = factory.newWorker(TASK_LIST);

        worker.registerWorkflowImplementationTypes(DiagnosticsWorkflowImpl.class);

        List<Object> activityImpls = new ArrayList<>(scanActivities);
        activityImpls.add(prioritize);
        activityImpls.add(notify);
        worker.registerActivitiesImplementations(activityImpls.toArray());

        factory.start();
        return factory;
    }

    @PostConstruct
    public void registerDomainIdempotent() throws Exception {
        try (IWorkflowService svc = workflowService()) {
            RegisterDomainRequest req = new RegisterDomainRequest()
                .setName(DOMAIN)
                .setWorkflowExecutionRetentionPeriodInDays(1);
            try {
                svc.RegisterDomain(req);
            } catch (DomainAlreadyExistsError ignored) {
                // first-run-only side effect — idempotent on subsequent boots
            }
        }
    }
}
```

> **`DiagnosticsWorkflowImpl` as a bean**: Cadence's workflow impl class is *instantiated by the worker per execution*, not by Spring — but injecting it here is for *type registration only* (`registerWorkflowImplementationTypes(Class<?>...)`). Mark the class `@Component` so Spring constructs a dummy instance whose `.getClass()` we pass to the worker; the worker ignores the instance and uses the type to instantiate fresh workflow objects per run. This is the canonical Cadence-with-Spring pattern. The Temporal starter hides exactly this dance.

> **`ScanActivity` marker interface**: Define a marker `public interface ScanActivity {}` and have all 5 `Scan*ActivityImpl` classes implement it. Spring's `List<ScanActivity>` injection then gives the config class all 5 in one shot — no per-activity wiring. Keeps `CadenceConfig` to ~40 lines and prevents the on-stage code from looking like a 7-arg constructor.

### Why this file is the talking point

On the §5.1 demo intro slide, paste the diff:

```
Temporal:                                  Cadence:
(no equivalent file — starter handles it)  CadenceConfig.java  (~40 lines)
                                            ├── IWorkflowService @Bean
                                            ├── WorkflowClient   @Bean
                                            ├── WorkerFactory    @Bean
                                            │   ├── new Worker(TASK_LIST)
                                            │   ├── registerWorkflowImplementationTypes(...)
                                            │   ├── registerActivitiesImplementations(...)
                                            │   └── factory.start()
                                            └── @PostConstruct registerDomain (idempotent)
```

Narration: *"Same SDK API surface — `@WorkflowMethod`, `Async.function`, `Promise.allOf`, `Workflow.sleep`. The difference is that you wire the worker yourself. Cadence is from 2017; Temporal's Spring Boot starter is from 2022. One file is the five years of ergonomics work."*

## 12. plan.md updates (already applied)

The Cadence-Java decision was applied to `plan.md` in the prior turn — Locked-Decisions table (line 12), timeline row (line 30), repo layout (lines 67–82), §5.0 idiomatic-naming bullets (lines 416–422), §5.1 Cadence subsection (the rewritten Cadence Java block), §7 pre-talk checklist (Maven `dependency:go-offline` for both `01-cadence` and `02-temporal`), §8 authoring order step 5 (copy-Temporal + swap imports + add `CadenceConfig`), §9 risks (Cassandra-cold-start now scoped to Cadence only).

This spec is the implementation contract for those changes. No further `plan.md` edits are required.

## 13. Mapping — Temporal vs Cadence cheat sheet

For on-stage code comparison slides (`02-languages.md` may want this):

| Temporal Java | Cadence Java |
|---|---|
| `import io.temporal.workflow.*` | `import com.uber.cadence.workflow.*` |
| `import io.temporal.activity.*` | `import com.uber.cadence.activity.*` |
| `@WorkflowInterface` + `@WorkflowMethod` | **same annotations**, different package |
| `@WorkflowImpl(taskQueues = "...")` (starter auto-discovery) | *(none — registered via `worker.registerWorkflowImplementationTypes(...)`)* |
| `@ActivityImpl(taskQueues = "...")` (starter auto-discovery) | *(none — registered via `worker.registerActivitiesImplementations(...)`)* |
| `Workflow.newActivityStub(I.class, opts)` | **same** |
| `Async.function(stub::m, input)` | **same** |
| `Promise.allOf(p1, p2, ...).get()` | **same** |
| `Workflow.sleep(Duration.ofSeconds(60))` | **same** |
| `Workflow.getLogger(C.class)` | **same** |
| `ActivityOptions.newBuilder().setStartToCloseTimeout(...)` | `new ActivityOptions.Builder().setScheduleToCloseTimeout(...).setStartToCloseTimeout(...)` (Cadence requires `scheduleToClose`) |
| `RetryOptions.newBuilder()` | `new RetryOptions.Builder()` |
| **namespace** (`spring.temporal.namespace: default`) | **domain** (`cadence.domain: default`) |
| **task queue** (`task-queue: diagnostics-tq`) | **task list** (`task-list: diagnostics-tl`) |
| `temporal-spring-boot-starter-alpha` auto-wires `WorkflowClient` + `WorkerFactory` | Manual `@Bean`s in `CadenceConfig` (no first-party starter) |
| Domain/namespace auto-exists in dev server | `RegisterDomain` required on first run (`@PostConstruct` idempotent) |
| `temporal server start-dev` (single binary, in-memory) | `docker compose up` (Cassandra + server + Web) |
| Default frontend port `7233` | Default frontend port `7933` |
| Web UI on `:8233` | Web UI on `:8088` |

## 14. Out of scope (first pass)

- **Workflow versioning.** Cadence has `Workflow.getVersion(changeId, minSupported, maxSupported)` (same API as Temporal — inherited at fork) but the canonical scenario doesn't require it.
- **Async activity completion / heartbeats.** Not needed for ≤60s scans.
- **Continue-as-new.** Workflow is short-lived (~70s end-to-end); no history-size concerns.
- **Integration tests.** Cadence has `TestWorkflowEnvironment` (same shape as Temporal's). Useful, but not on the critical path for the talk.
- **MySQL persistence.** Cassandra is the canonical Cadence store and what every example uses; don't introduce a second persistence option for a 4-minute demo.
- **Authentication, metrics, tracing.** Stage demo only.

## 15. Risks

| Risk | Mitigation |
|---|---|
| Cassandra cold start on stage (>2 min from `docker compose up` to healthy frontend) | Start `docker compose up` during the **Dim 4 — Performance** slide (~3 min upstream of the demo). The compose `depends_on: cassandra: service_healthy` ensures the Cadence server doesn't try to connect early. |
| `ubercadence/server` image tag drifts; `:latest` breaks unrelated things | Pin a known-good tag in `docker-compose.yml`; verify end-to-end on the talk laptop a week before |
| Cadence Java client `3.x` records-with-Jackson compatibility | Cadence client uses Jackson by default; records are first-class since Jackson 2.12. Verify with a smoke test pre-talk; fall back to classic POJOs with `@JsonCreator` if anything breaks |
| Severity enum JSON mismatch with `.NET` strings (`red_alert` vs `RED_ALERT`) | Same fix as Temporal — `@JsonProperty("red_alert")` per enum constant. Use the same `Severity.java` file content as the Temporal demo. |
| `@PostConstruct registerDomainIdempotent` runs before the server is reachable | The `@PostConstruct` retries on `ServiceBusyError` / connection refused with bounded backoff (3 attempts, 2s apart). If still failing, log a clear "Cadence server not reachable — is `docker compose up` complete?" and let Spring Boot exit with a non-zero status. |
| `WorkerFactory.start()` called from `@Bean` method may interfere with Spring's lifecycle ordering | `WorkerFactory` bean has `destroyMethod = "shutdownNow"` to ensure clean shutdown on Spring context close. Start order: `IWorkflowService` → `WorkflowClient` → `WorkerFactory` (which starts the worker). The `@PostConstruct registerDomain` runs first because it's in the same `@Configuration` class. |
| Audience asks "why not use the community `cadence-spring-boot-starter`?" | Pre-bake the answer: "It's community-maintained — last release predates the talk. For a stage demo, depending on a starter that hasn't shipped in 18 months is the wrong risk. Forty lines of `@Bean` wiring is cheaper than a broken extension at a conference." |
| Audience asks "isn't Temporal's `start-dev` just easier?" | Lean into it: "Yes — and that's *the* dimension-3 finding. Cadence is the ancestor; Temporal is the fork with five years of ergonomics work on top. This demo is showing you the lineage and the cost-of-entry delta in the same five minutes." |

## 16. Verification checklist (before declaring the demo done)

- [ ] `docker compose up` brings Cassandra + Cadence server + Cadence Web to healthy in <3 min from cold pull.
- [ ] Cadence Web reachable at `http://localhost:8088`; `default` domain visible.
- [ ] `./mvnw spring-boot:run` starts in <15s; `CadenceConfig.@PostConstruct` registers the domain idempotently (no failure on second boot).
- [ ] Worker registered on task list `diagnostics-tl` (visible in Cadence Web → Task Lists view).
- [ ] `POST /start` returns `200 OK` with `{ "workflowId": "enterprise-ncc-1701" }`.
- [ ] In Cadence Web, the 5 scan activities complete near-instantly (no artificial delay), then a `TimerStarted` event appears.
- [ ] During the 60s timer, killing the Spring Boot process and restarting it does not advance the workflow until the timer fires; after it fires, prioritization runs and `NotifyBridgeActivity` is invoked.
- [ ] The `🚨 BRIDGE ALERT` line appears in the Spring Boot stdout at WARN level with the canonical message format.
- [ ] `GET /status/enterprise-ncc-1701` after completion returns a `DiagnosticsOutput` with non-null `bridgeNotification.message` (because severity reliably hits `red_alert`).
- [ ] Run the workflow 10× in a row — `bridgeNotification` is never null (rigging holds).
- [ ] Total wall-clock from `docker compose up` cold start to completed workflow: under 5 minutes.
- [ ] **Sister-demo check**: open `demos/01-cadence/.../DiagnosticsWorkflowImpl.java` and `demos/02-temporal/.../DiagnosticsWorkflowImpl.java` side-by-side. The diff is limited to the imports, the `ActivityOptions` builder shape, and the missing `@WorkflowImpl` annotation on the Cadence side. The `runDiagnostics` method body is byte-for-byte identical.
- [ ] **Sister-demo check**: the model records and `MockAnalysisGenerator.java` are byte-for-byte identical between `01-cadence/` and `02-temporal/`.
