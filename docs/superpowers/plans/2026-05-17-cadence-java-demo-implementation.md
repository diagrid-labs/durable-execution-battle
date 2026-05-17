# Cadence (Java) Demo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the USS Enterprise diagnostics workflow demo in **Cadence Java** (Spring Boot 3, Java 21, Maven) under `demos/01-cadence/`, matching the canonical scenario and cross-demo invariants of the .NET reference at `demos/04-dapr/`, and structurally mirroring the sister Temporal demo at `demos/02-temporal/` so the on-stage Cadence↔Temporal diff is limited to imports + one config class + the docker-compose cluster.

**Architecture:** Single Maven module hosting one Spring Boot process that contains both the Cadence worker (registered manually via a `CadenceConfig` `@Configuration` class — Cadence has no first-party Spring Boot starter) and a REST controller. One `@WorkflowInterface` (`DiagnosticsWorkflow`) drives 5 parallel scan activities, a 60-second durable timer (the on-stage kill point), a deterministic prioritizer, and a conditional bridge notification. State lives in a docker-compose cluster (Cassandra 4 + `ubercadence/server` + `ubercadence/web`). Java records + Jackson handle the wire format; the `Severity` enum uses `@JsonProperty` to match the .NET strings (`green` / `yellow_alert` / `red_alert`). The `default` domain is auto-registered on app startup by an idempotent `ApplicationRunner`.

**Tech Stack:** Java 21 LTS · Maven (with wrapper) · Spring Boot 3.4.x · `com.uber.cadence:cadence-client` 3.13.x · Jackson (default Spring Boot) · JUnit 5 + `com.uber.cadence:cadence-client:tests` (for `TestWorkflowEnvironment`) · Docker / docker-compose · Cassandra 4 · `ubercadence/server` + `ubercadence/web`.

**Reference (scenario only, not style):** `demos/04-dapr/EnterpriseDiagnostics.ApiService/`. Activity names, severity strings, subsystem ids, health range, retry policy shape, bridge message format, and workflow id must match.

**Reference (sister demo for symmetry):** `demos/02-temporal/` — keep model records, `MockAnalysisGenerator`, `PrioritizeIssuesActivity` body, and `NotifyBridgeActivity` body **byte-for-byte identical** so the on-stage source diff is limited to imports + `CadenceConfig`. Do **not** transliterate C# style.

---

## File Structure

```
demos/01-cadence/
├── README.md
├── pom.xml
├── mvnw, mvnw.cmd, .mvn/wrapper/...
├── docker-compose.yml
├── enterprise-diagnostics.http
├── .gitignore
└── src/
    ├── main/
    │   ├── java/io/diagrid/enterprisediagnostics/
    │   │   ├── EnterpriseDiagnosticsApplication.java
    │   │   ├── config/
    │   │   │   ├── CadenceProperties.java
    │   │   │   ├── CadenceConfig.java
    │   │   │   └── DomainRegistrar.java
    │   │   ├── model/
    │   │   │   ├── DiagnosticsInput.java
    │   │   │   ├── DiagnosticsOutput.java
    │   │   │   ├── ScanInput.java
    │   │   │   ├── ScanResult.java
    │   │   │   ├── PrioritizeInput.java
    │   │   │   ├── Priority.java
    │   │   │   ├── Severity.java
    │   │   │   ├── BridgeNotificationInput.java
    │   │   │   └── BridgeNotificationResult.java
    │   │   ├── activity/
    │   │   │   ├── ScanActivity.java                # marker interface
    │   │   │   ├── MockAnalysisGenerator.java
    │   │   │   ├── ScanWarpCoreActivity.java        + Impl
    │   │   │   ├── ScanShieldsActivity.java         + Impl
    │   │   │   ├── ScanLifeSupportActivity.java     + Impl
    │   │   │   ├── ScanSensorsActivity.java         + Impl
    │   │   │   ├── ScanWeaponsActivity.java         + Impl
    │   │   │   ├── PrioritizeIssuesActivity.java    + Impl
    │   │   │   └── NotifyBridgeActivity.java        + Impl
    │   │   ├── workflow/
    │   │   │   ├── DiagnosticsWorkflow.java
    │   │   │   └── DiagnosticsWorkflowImpl.java
    │   │   └── api/
    │   │       └── DiagnosticsController.java
    │   └── resources/
    │       └── application.yml
    └── test/java/io/diagrid/enterprisediagnostics/
        ├── model/SeverityJsonTest.java
        ├── activity/MockAnalysisGeneratorTest.java
        ├── activity/PrioritizeIssuesActivityImplTest.java
        ├── activity/NotifyBridgeActivityImplTest.java
        └── workflow/DiagnosticsWorkflowImplTest.java
```

Single responsibility per file. Activity interface and impl are split per Java convention. Tests live mirror to the source package.

The **only** structural deltas vs the Temporal demo are:
1. `config/CadenceConfig.java` + `config/CadenceProperties.java` + `config/DomainRegistrar.java` (Temporal has no `config/` — the starter handles it).
2. `activity/ScanActivity.java` (marker interface used to inject all 5 scan impls in one `List<ScanActivity>`).
3. `docker-compose.yml` (Temporal uses `temporal server start-dev`).

Everything else has a 1:1 file mapping with the Temporal demo.

---

## Task 1: Maven + Spring Boot skeleton

**Files:**
- Create: `demos/01-cadence/pom.xml`
- Create: `demos/01-cadence/mvnw`, `demos/01-cadence/mvnw.cmd`, `demos/01-cadence/.mvn/wrapper/maven-wrapper.properties`
- Create: `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/EnterpriseDiagnosticsApplication.java`
- Create: `demos/01-cadence/src/main/resources/application.yml` (placeholder — final values in Task 11)
- Create: `demos/01-cadence/.gitignore`

- [ ] **Step 1: Create `pom.xml`**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<project xmlns="http://maven.apache.org/POM/4.0.0"
         xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xsi:schemaLocation="http://maven.apache.org/POM/4.0.0 https://maven.apache.org/xsd/maven-4.0.0.xsd">
    <modelVersion>4.0.0</modelVersion>

    <parent>
        <groupId>org.springframework.boot</groupId>
        <artifactId>spring-boot-starter-parent</artifactId>
        <version>3.4.0</version>
        <relativePath/>
    </parent>

    <groupId>io.diagrid</groupId>
    <artifactId>enterprise-diagnostics-cadence</artifactId>
    <version>0.1.0</version>
    <name>enterprise-diagnostics-cadence</name>

    <properties>
        <java.version>21</java.version>
        <cadence.version>3.13.0</cadence.version>
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
        <dependency>
            <groupId>org.springframework.boot</groupId>
            <artifactId>spring-boot-starter-test</artifactId>
            <scope>test</scope>
        </dependency>
    </dependencies>

    <build>
        <plugins>
            <plugin>
                <groupId>org.springframework.boot</groupId>
                <artifactId>spring-boot-maven-plugin</artifactId>
            </plugin>
        </plugins>
    </build>
</project>
```

- [ ] **Step 2: Generate Maven wrapper**

Run:
```bash
cd demos/01-cadence && mvn -N wrapper:wrapper -Dmaven=3.9.9
```
Expected: creates `mvnw`, `mvnw.cmd`, `.mvn/wrapper/maven-wrapper.properties`.

- [ ] **Step 3: Create `EnterpriseDiagnosticsApplication.java`**

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/EnterpriseDiagnosticsApplication.java`:

```java
package io.diagrid.enterprisediagnostics;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

@SpringBootApplication
public class EnterpriseDiagnosticsApplication {
    public static void main(String[] args) {
        SpringApplication.run(EnterpriseDiagnosticsApplication.class, args);
    }
}
```

- [ ] **Step 4: Create placeholder `application.yml`**

`demos/01-cadence/src/main/resources/application.yml`:

```yaml
server:
  port: 8080

logging:
  level:
    root: INFO
    io.diagrid.enterprisediagnostics: INFO
    com.uber.cadence: WARN
```

(Cadence-specific keys land in Task 11.)

- [ ] **Step 5: Create `.gitignore`**

`demos/01-cadence/.gitignore`:

```
target/
.idea/
*.iml
.vscode/
.DS_Store
HELP.md
```

- [ ] **Step 6: Verify the skeleton builds**

Run:
```bash
cd demos/01-cadence && ./mvnw -q compile
```
Expected: BUILD SUCCESS. No warnings about missing Cadence types yet — we only have the application class.

- [ ] **Step 7: Verify it starts and exits cleanly**

Run:
```bash
cd demos/01-cadence && timeout 15 ./mvnw -q spring-boot:run || echo "exited as expected"
```
Expected: Spring Boot banner, `Started EnterpriseDiagnosticsApplication`, then exits on timeout. No stack traces.

- [ ] **Step 8: Commit**

```bash
git add demos/01-cadence/pom.xml demos/01-cadence/mvnw demos/01-cadence/mvnw.cmd demos/01-cadence/.mvn demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/EnterpriseDiagnosticsApplication.java demos/01-cadence/src/main/resources/application.yml demos/01-cadence/.gitignore
git commit -m "chore(01-cadence): scaffold Maven + Spring Boot 3 skeleton"
```

---

## Task 2: docker-compose cluster (Cassandra + Cadence server + Cadence Web)

**Files:**
- Create: `demos/01-cadence/docker-compose.yml`

- [ ] **Step 1: Create `docker-compose.yml`**

`demos/01-cadence/docker-compose.yml`:

```yaml
services:
  cassandra:
    image: cassandra:4.1
    container_name: cadence-cassandra
    ports:
      - "9042:9042"
    environment:
      CASSANDRA_LISTEN_ADDRESS: auto
      MAX_HEAP_SIZE: 512M
      HEAP_NEWSIZE: 100M
    healthcheck:
      test: ["CMD", "cqlsh", "-e", "describe keyspaces"]
      interval: 15s
      timeout: 5s
      retries: 12

  cadence:
    image: ubercadence/server:1.2.13
    container_name: cadence-server
    ports:
      - "7933:7933"
      - "7934:7934"
      - "7935:7935"
      - "7939:7939"
    environment:
      CASSANDRA_SEEDS: cassandra
      LOG_LEVEL: warn
    depends_on:
      cassandra:
        condition: service_healthy

  cadence-web:
    image: ubercadence/web:4.1.4
    container_name: cadence-web
    ports:
      - "8088:8088"
    environment:
      CADENCE_GRPC_PEERS: cadence:7833
      CADENCE_GRPC_SERVICES_NAME: cadence-frontend
    depends_on:
      - cadence
```

- [ ] **Step 2: Start the cluster**

Run (in a separate terminal):
```bash
cd demos/01-cadence && docker compose up -d
```
Expected: `cadence-cassandra`, `cadence-server`, `cadence-web` start. Cassandra takes 30–90s to become healthy.

- [ ] **Step 3: Wait for Cassandra to be healthy**

Run:
```bash
until docker inspect --format='{{.State.Health.Status}}' cadence-cassandra | grep -q healthy; do sleep 5; done && echo "cassandra healthy"
```
Expected: prints `cassandra healthy` within ~2 minutes.

- [ ] **Step 4: Verify Cadence frontend is reachable**

Run:
```bash
curl -fsS http://localhost:7933 -o /dev/null || echo "frontend port open (TChannel, not HTTP)"
```
Expected: TCP-level open. (Cadence frontend isn't HTTP, so curl will not 200; the test is just that the port is accepting connections.)

Better check:
```bash
docker exec cadence-server /etc/cadence/cadence-cli --address cadence:7833 --domain default domain describe 2>&1 | head -5
```
Expected: `domain not exists` (we haven't registered it yet) — this *confirms* the frontend responded to a gRPC call.

- [ ] **Step 5: Verify Cadence Web UI**

Open `http://localhost:8088` in a browser. Expected: Cadence Web loads. Searching for any domain returns empty (we haven't registered `default` yet — Task 10 will handle that programmatically).

- [ ] **Step 6: Commit**

```bash
git add demos/01-cadence/docker-compose.yml
git commit -m "feat(01-cadence): add docker-compose cluster (Cassandra + Cadence + Web)"
```

> **Tip:** Leave `docker compose up -d` running through the rest of the plan — the Spring Boot tests in later tasks use `TestWorkflowEnvironment` (in-memory) so they don't need the cluster, but the end-to-end task does. If you want to free RAM mid-build, `docker compose stop` and `docker compose start` later — the Cassandra keyspace persists in the volume.

---

## Task 3: Model records (data only — no tests for plain records)

**Files:**
- Create all 9 model files under `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/model/`

The records below have **no behavior** — they're data carriers. No unit tests are written for them; Task 4 covers `Severity` JSON behavior with tests.

- [ ] **Step 1: Create `DiagnosticsInput.java`**

```java
package io.diagrid.enterprisediagnostics.model;

public record DiagnosticsInput(
    String id,
    String shipName,
    String diagnosticsDate,
    String engineerName) {}
```

- [ ] **Step 2: Create `ScanInput.java`**

```java
package io.diagrid.enterprisediagnostics.model;

public record ScanInput(String shipName, String subsystem) {}
```

- [ ] **Step 3: Create `ScanResult.java`**

```java
package io.diagrid.enterprisediagnostics.model;

import java.util.List;

public record ScanResult(String subsystem, int health, List<String> anomalies) {}
```

- [ ] **Step 4: Create `PrioritizeInput.java`**

```java
package io.diagrid.enterprisediagnostics.model;

import java.util.List;

public record PrioritizeInput(String shipName, List<ScanResult> scans) {}
```

- [ ] **Step 5: Create `Priority.java`**

```java
package io.diagrid.enterprisediagnostics.model;

public record Priority(Severity severity, String summary) {}
```

- [ ] **Step 6: Create `BridgeNotificationInput.java`**

```java
package io.diagrid.enterprisediagnostics.model;

public record BridgeNotificationInput(String shipName, String engineerName, Priority priority) {}
```

- [ ] **Step 7: Create `BridgeNotificationResult.java`**

```java
package io.diagrid.enterprisediagnostics.model;

public record BridgeNotificationResult(String message) {}
```

- [ ] **Step 8: Create `DiagnosticsOutput.java`**

```java
package io.diagrid.enterprisediagnostics.model;

public record DiagnosticsOutput(
    String shipName,
    String diagnosticsDate,
    String engineerName,
    Priority priority,
    BridgeNotificationResult bridgeNotification) {}
```

> `bridgeNotification` is intentionally **nullable** — the green path leaves it null. Jackson serializes null fields as `null` by default; do not configure `Include.NON_NULL` globally, the audience needs to see the field exist with `null` so the conditional branch is observable.

- [ ] **Step 9: Compile**

Run: `cd demos/01-cadence && ./mvnw -q compile`
Expected: BUILD SUCCESS. Cannot pass yet — `Severity` referenced by `Priority` is missing. Task 4 creates it.

- [ ] **Step 10: Defer compile/commit until after Task 4**

Don't commit yet — the build won't pass until `Severity` exists.

---

## Task 4: `Severity` enum + Jackson JSON test (TDD)

**Files:**
- Create: `demos/01-cadence/src/test/java/io/diagrid/enterprisediagnostics/model/SeverityJsonTest.java`
- Create: `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/model/Severity.java`

- [ ] **Step 1: Write the failing test**

`demos/01-cadence/src/test/java/io/diagrid/enterprisediagnostics/model/SeverityJsonTest.java`:

```java
package io.diagrid.enterprisediagnostics.model;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.assertEquals;

class SeverityJsonTest {

    private final ObjectMapper mapper = new ObjectMapper();

    @Test
    void serializesGreenAsLowercase() throws Exception {
        assertEquals("\"green\"", mapper.writeValueAsString(Severity.GREEN));
    }

    @Test
    void serializesYellowAlertAsSnakeCase() throws Exception {
        assertEquals("\"yellow_alert\"", mapper.writeValueAsString(Severity.YELLOW_ALERT));
    }

    @Test
    void serializesRedAlertAsSnakeCase() throws Exception {
        assertEquals("\"red_alert\"", mapper.writeValueAsString(Severity.RED_ALERT));
    }

    @Test
    void deserializesGreenFromLowercase() throws Exception {
        assertEquals(Severity.GREEN, mapper.readValue("\"green\"", Severity.class));
    }

    @Test
    void deserializesYellowAlertFromSnakeCase() throws Exception {
        assertEquals(Severity.YELLOW_ALERT, mapper.readValue("\"yellow_alert\"", Severity.class));
    }

    @Test
    void deserializesRedAlertFromSnakeCase() throws Exception {
        assertEquals(Severity.RED_ALERT, mapper.readValue("\"red_alert\"", Severity.class));
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `cd demos/01-cadence && ./mvnw -q -Dtest=SeverityJsonTest test`
Expected: FAIL — `Severity` symbol cannot be resolved.

- [ ] **Step 3: Create `Severity.java`**

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/model/Severity.java`:

```java
package io.diagrid.enterprisediagnostics.model;

import com.fasterxml.jackson.annotation.JsonProperty;

public enum Severity {
    @JsonProperty("green")        GREEN,
    @JsonProperty("yellow_alert") YELLOW_ALERT,
    @JsonProperty("red_alert")    RED_ALERT;
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `cd demos/01-cadence && ./mvnw -q -Dtest=SeverityJsonTest test`
Expected: PASS (6/6).

- [ ] **Step 5: Compile the full project**

Run: `cd demos/01-cadence && ./mvnw -q compile`
Expected: BUILD SUCCESS — all 9 model files plus `Severity` now compile.

- [ ] **Step 6: Commit**

```bash
git add demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/model/ demos/01-cadence/src/test/java/io/diagrid/enterprisediagnostics/model/SeverityJsonTest.java
git commit -m "feat(01-cadence): add model records + Severity enum with Jackson snake_case wire format"
```

---

## Task 5: `MockAnalysisGenerator` (TDD)

**Files:**
- Create: `demos/01-cadence/src/test/java/io/diagrid/enterprisediagnostics/activity/MockAnalysisGeneratorTest.java`
- Create: `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/MockAnalysisGenerator.java`

- [ ] **Step 1: Write the failing test**

`demos/01-cadence/src/test/java/io/diagrid/enterprisediagnostics/activity/MockAnalysisGeneratorTest.java`:

```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanResult;
import org.junit.jupiter.api.RepeatedTest;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.junit.jupiter.api.Assertions.*;

class MockAnalysisGeneratorTest {

    private static final List<String> CANDIDATES = List.of("a", "b", "c", "d", "e");

    @RepeatedTest(50)
    void healthIsAlwaysBetween30And65Inclusive() {
        ScanResult r = MockAnalysisGenerator.generate("warp_core", CANDIDATES);
        assertTrue(r.health() >= 30, "health was " + r.health());
        assertTrue(r.health() <= 65, "health was " + r.health());
    }

    @RepeatedTest(50)
    void returnsBetween1And3AnomaliesInclusive() {
        ScanResult r = MockAnalysisGenerator.generate("shields", CANDIDATES);
        assertTrue(r.anomalies().size() >= 1, "got " + r.anomalies().size());
        assertTrue(r.anomalies().size() <= 3, "got " + r.anomalies().size());
    }

    @RepeatedTest(50)
    void anomaliesAreDrawnFromCandidates() {
        ScanResult r = MockAnalysisGenerator.generate("sensors", CANDIDATES);
        for (String a : r.anomalies()) {
            assertTrue(CANDIDATES.contains(a), "unexpected anomaly: " + a);
        }
    }

    @Test
    void subsystemIsReturnedVerbatim() {
        ScanResult r = MockAnalysisGenerator.generate("life_support", CANDIDATES);
        assertEquals("life_support", r.subsystem());
    }

    @RepeatedTest(50)
    void anomaliesAreUnique() {
        ScanResult r = MockAnalysisGenerator.generate("weapons", CANDIDATES);
        assertEquals(r.anomalies().size(), r.anomalies().stream().distinct().count(),
            "duplicates in anomalies list");
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `cd demos/01-cadence && ./mvnw -q -Dtest=MockAnalysisGeneratorTest test`
Expected: FAIL — `MockAnalysisGenerator` symbol cannot be resolved.

- [ ] **Step 3: Create `MockAnalysisGenerator.java`**

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/MockAnalysisGenerator.java`:

```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanResult;

import java.security.SecureRandom;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

public final class MockAnalysisGenerator {

    private static final SecureRandom RANDOM = new SecureRandom();

    private MockAnalysisGenerator() {}

    public static ScanResult generate(String subsystem, List<String> candidateAnomalies) {
        int health = RANDOM.nextInt(30, 66);
        int maxAnomalies = (health >= 60) ? 1 : (health >= 45) ? 2 : 3;
        int anomalyCount = RANDOM.nextInt(1, maxAnomalies + 1);

        List<String> shuffled = new ArrayList<>(candidateAnomalies);
        Collections.shuffle(shuffled, RANDOM);
        List<String> picked = shuffled.subList(0, anomalyCount);

        return new ScanResult(subsystem, health, List.copyOf(picked));
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `cd demos/01-cadence && ./mvnw -q -Dtest=MockAnalysisGeneratorTest test`
Expected: PASS (all repeats green).

- [ ] **Step 5: Commit**

```bash
git add demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/MockAnalysisGenerator.java demos/01-cadence/src/test/java/io/diagrid/enterprisediagnostics/activity/MockAnalysisGeneratorTest.java
git commit -m "feat(01-cadence): add MockAnalysisGenerator with biased-low health range (30..65) and 1..3 anomalies"
```

---

## Task 6: `PrioritizeIssuesActivity` (interface + impl, TDD)

**Files:**
- Create: `demos/01-cadence/src/test/java/io/diagrid/enterprisediagnostics/activity/PrioritizeIssuesActivityImplTest.java`
- Create: `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/PrioritizeIssuesActivity.java`
- Create: `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/PrioritizeIssuesActivityImpl.java`

- [ ] **Step 1: Write the failing test**

`demos/01-cadence/src/test/java/io/diagrid/enterprisediagnostics/activity/PrioritizeIssuesActivityImplTest.java`:

```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.PrioritizeInput;
import io.diagrid.enterprisediagnostics.model.Priority;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import io.diagrid.enterprisediagnostics.model.Severity;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.junit.jupiter.api.Assertions.*;

class PrioritizeIssuesActivityImplTest {

    private final PrioritizeIssuesActivity activity = new PrioritizeIssuesActivityImpl();

    @Test
    void redAlertWhenMinHealthBelow50() {
        Priority p = activity.execute(new PrioritizeInput("USS Enterprise", List.of(
            new ScanResult("warp_core", 45, List.of("x")),
            new ScanResult("shields",   80, List.of()))));
        assertEquals(Severity.RED_ALERT, p.severity());
        assertTrue(p.summary().startsWith("warp_core"), "summary: " + p.summary());
    }

    @Test
    void yellowAlertWhenMinHealthBelow75ButNotBelow50() {
        Priority p = activity.execute(new PrioritizeInput("USS Enterprise", List.of(
            new ScanResult("warp_core", 70, List.of("x")),
            new ScanResult("shields",   80, List.of()))));
        assertEquals(Severity.YELLOW_ALERT, p.severity());
    }

    @Test
    void greenWhenAllHealthAtOrAbove75() {
        Priority p = activity.execute(new PrioritizeInput("USS Enterprise", List.of(
            new ScanResult("warp_core", 90, List.of()),
            new ScanResult("shields",   75, List.of()))));
        assertEquals(Severity.GREEN, p.severity());
    }

    @Test
    void boundaryHealthEquals50IsYellowNotRed() {
        Priority p = activity.execute(new PrioritizeInput("USS Enterprise", List.of(
            new ScanResult("warp_core", 50, List.of()),
            new ScanResult("shields",   80, List.of()))));
        assertEquals(Severity.YELLOW_ALERT, p.severity());
    }

    @Test
    void boundaryHealthEquals75IsGreenNotYellow() {
        Priority p = activity.execute(new PrioritizeInput("USS Enterprise", List.of(
            new ScanResult("warp_core", 75, List.of()),
            new ScanResult("shields",   80, List.of()))));
        assertEquals(Severity.GREEN, p.severity());
    }

    @Test
    void summaryNamesWorstSubsystemAndCountsBelowThreshold() {
        Priority p = activity.execute(new PrioritizeInput("USS Enterprise", List.of(
            new ScanResult("warp_core", 40, List.of()),
            new ScanResult("shields",   70, List.of()),
            new ScanResult("sensors",   80, List.of()))));
        assertTrue(p.summary().contains("warp_core"));
        assertTrue(p.summary().contains("2"), "expected count=2 of subsystems <75; got: " + p.summary());
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `cd demos/01-cadence && ./mvnw -q -Dtest=PrioritizeIssuesActivityImplTest test`
Expected: FAIL — types `PrioritizeIssuesActivity` / `Impl` cannot be resolved.

- [ ] **Step 3: Create the interface**

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/PrioritizeIssuesActivity.java`:

```java
package io.diagrid.enterprisediagnostics.activity;

import com.uber.cadence.activity.ActivityInterface;
import com.uber.cadence.activity.ActivityMethod;
import io.diagrid.enterprisediagnostics.model.PrioritizeInput;
import io.diagrid.enterprisediagnostics.model.Priority;

@ActivityInterface
public interface PrioritizeIssuesActivity {
    @ActivityMethod
    Priority execute(PrioritizeInput input);
}
```

- [ ] **Step 4: Create the impl**

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/PrioritizeIssuesActivityImpl.java`:

```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.PrioritizeInput;
import io.diagrid.enterprisediagnostics.model.Priority;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import io.diagrid.enterprisediagnostics.model.Severity;
import org.springframework.stereotype.Component;

import java.util.Comparator;

@Component
public class PrioritizeIssuesActivityImpl implements PrioritizeIssuesActivity {

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
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `cd demos/01-cadence && ./mvnw -q -Dtest=PrioritizeIssuesActivityImplTest test`
Expected: PASS (6/6).

- [ ] **Step 6: Commit**

```bash
git add demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/PrioritizeIssuesActivity.java demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/PrioritizeIssuesActivityImpl.java demos/01-cadence/src/test/java/io/diagrid/enterprisediagnostics/activity/PrioritizeIssuesActivityImplTest.java
git commit -m "feat(01-cadence): add PrioritizeIssuesActivity with severity thresholds <50→red, <75→yellow, else green"
```

---

## Task 7: `NotifyBridgeActivity` (interface + impl, TDD)

**Files:**
- Create: `demos/01-cadence/src/test/java/io/diagrid/enterprisediagnostics/activity/NotifyBridgeActivityImplTest.java`
- Create: `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/NotifyBridgeActivity.java`
- Create: `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/NotifyBridgeActivityImpl.java`

- [ ] **Step 1: Write the failing test**

`demos/01-cadence/src/test/java/io/diagrid/enterprisediagnostics/activity/NotifyBridgeActivityImplTest.java`:

```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.BridgeNotificationInput;
import io.diagrid.enterprisediagnostics.model.BridgeNotificationResult;
import io.diagrid.enterprisediagnostics.model.Priority;
import io.diagrid.enterprisediagnostics.model.Severity;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.*;

class NotifyBridgeActivityImplTest {

    private final NotifyBridgeActivity activity = new NotifyBridgeActivityImpl();

    @Test
    void messageFormatMatchesCanonicalString() {
        BridgeNotificationResult r = activity.execute(new BridgeNotificationInput(
            "USS Enterprise NCC-1701-D",
            "Geordi La Forge",
            new Priority(Severity.RED_ALERT, "warp_core critical at 40% health; 2 subsystem(s) below threshold")));

        assertEquals(
            "🚨 BRIDGE ALERT — USS USS Enterprise NCC-1701-D — RED_ALERT: warp_core critical at 40% health; 2 subsystem(s) below threshold (engineer: Geordi La Forge)",
            r.message());
    }

    @Test
    void usesEnumNameNotJsonValueForOnStageReadability() {
        BridgeNotificationResult r = activity.execute(new BridgeNotificationInput(
            "Voyager",
            "B'Elanna",
            new Priority(Severity.YELLOW_ALERT, "shields at 70% health; 1 subsystem(s) below threshold")));

        assertTrue(r.message().contains("YELLOW_ALERT"),
            "expected enum.name() in message for visibility; got: " + r.message());
    }
}
```

> The on-stage banner uses `severity.name()` (the SCREAMING_SNAKE enum constant) for visibility — NOT the snake_case JSON wire form. This matches the .NET reference and the Temporal demo.

- [ ] **Step 2: Run the test to verify it fails**

Run: `cd demos/01-cadence && ./mvnw -q -Dtest=NotifyBridgeActivityImplTest test`
Expected: FAIL — types cannot be resolved.

- [ ] **Step 3: Create the interface**

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/NotifyBridgeActivity.java`:

```java
package io.diagrid.enterprisediagnostics.activity;

import com.uber.cadence.activity.ActivityInterface;
import com.uber.cadence.activity.ActivityMethod;
import io.diagrid.enterprisediagnostics.model.BridgeNotificationInput;
import io.diagrid.enterprisediagnostics.model.BridgeNotificationResult;

@ActivityInterface
public interface NotifyBridgeActivity {
    @ActivityMethod
    BridgeNotificationResult execute(BridgeNotificationInput input);
}
```

- [ ] **Step 4: Create the impl**

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/NotifyBridgeActivityImpl.java`:

```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.BridgeNotificationInput;
import io.diagrid.enterprisediagnostics.model.BridgeNotificationResult;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

@Component
public class NotifyBridgeActivityImpl implements NotifyBridgeActivity {

    private static final Logger log = LoggerFactory.getLogger(NotifyBridgeActivityImpl.class);

    @Override
    public BridgeNotificationResult execute(BridgeNotificationInput input) {
        String message = "🚨 BRIDGE ALERT — USS %s — %s: %s (engineer: %s)".formatted(
            input.shipName(),
            input.priority().severity().name(),
            input.priority().summary(),
            input.engineerName());
        log.warn(message);
        return new BridgeNotificationResult(message);
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `cd demos/01-cadence && ./mvnw -q -Dtest=NotifyBridgeActivityImplTest test`
Expected: PASS (2/2).

- [ ] **Step 6: Commit**

```bash
git add demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/NotifyBridgeActivity.java demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/NotifyBridgeActivityImpl.java demos/01-cadence/src/test/java/io/diagrid/enterprisediagnostics/activity/NotifyBridgeActivityImplTest.java
git commit -m "feat(01-cadence): add NotifyBridgeActivity with canonical banner format"
```

---

## Task 8: `ScanActivity` marker + 5 scan activity interfaces + impls

**Files:**
- Create: `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/ScanActivity.java`
- Create 10 files: 5 scan interfaces (`Scan{X}Activity.java`) + 5 impls (`Scan{X}ActivityImpl.java`) under the same package.

No TDD here — each impl is a thin one-liner delegating to `MockAnalysisGenerator` (already TDD'd). Wiring is covered by the workflow test in Task 12.

- [ ] **Step 1: Create the `ScanActivity` marker interface**

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/ScanActivity.java`:

```java
package io.diagrid.enterprisediagnostics.activity;

/**
 * Marker interface implemented by all five Scan*ActivityImpl classes so that
 * Spring can inject the full set as List<ScanActivity> into CadenceConfig.
 * Not annotated with @ActivityInterface — that lives on each concrete sub-interface.
 */
public interface ScanActivity {}
```

- [ ] **Step 2: Create `ScanWarpCoreActivity` + Impl**

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/ScanWarpCoreActivity.java`:

```java
package io.diagrid.enterprisediagnostics.activity;

import com.uber.cadence.activity.ActivityInterface;
import com.uber.cadence.activity.ActivityMethod;
import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;

@ActivityInterface
public interface ScanWarpCoreActivity {
    @ActivityMethod
    ScanResult execute(ScanInput input);
}
```

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/ScanWarpCoreActivityImpl.java`:

```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
public class ScanWarpCoreActivityImpl implements ScanWarpCoreActivity, ScanActivity {

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

- [ ] **Step 3: Create `ScanShieldsActivity` + Impl**

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/ScanShieldsActivity.java`:

```java
package io.diagrid.enterprisediagnostics.activity;

import com.uber.cadence.activity.ActivityInterface;
import com.uber.cadence.activity.ActivityMethod;
import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;

@ActivityInterface
public interface ScanShieldsActivity {
    @ActivityMethod
    ScanResult execute(ScanInput input);
}
```

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/ScanShieldsActivityImpl.java`:

```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
public class ScanShieldsActivityImpl implements ScanShieldsActivity, ScanActivity {

    private static final List<String> CANDIDATES = List.of(
        "shield emitter 4 power flicker",
        "ablative armor delamination forward dorsal",
        "deflector phase polarization drift 0.7°",
        "graviton field harmonic instability",
        "structural integrity field starboard nacelle pylon");

    @Override
    public ScanResult execute(ScanInput input) {
        return MockAnalysisGenerator.generate(input.subsystem(), CANDIDATES);
    }
}
```

- [ ] **Step 4: Create `ScanLifeSupportActivity` + Impl**

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/ScanLifeSupportActivity.java`:

```java
package io.diagrid.enterprisediagnostics.activity;

import com.uber.cadence.activity.ActivityInterface;
import com.uber.cadence.activity.ActivityMethod;
import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;

@ActivityInterface
public interface ScanLifeSupportActivity {
    @ActivityMethod
    ScanResult execute(ScanInput input);
}
```

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/ScanLifeSupportActivityImpl.java`:

```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
public class ScanLifeSupportActivityImpl implements ScanLifeSupportActivity, ScanActivity {

    private static final List<String> CANDIDATES = List.of(
        "CO2 scrubber efficiency 87% on deck 12",
        "atmospheric processor inlet contamination",
        "gravity plating fluctuation crew quarters 4F",
        "thermal regulation cold-spot deck 22",
        "emergency oxygen reserve seal #3 micro-leak");

    @Override
    public ScanResult execute(ScanInput input) {
        return MockAnalysisGenerator.generate(input.subsystem(), CANDIDATES);
    }
}
```

- [ ] **Step 5: Create `ScanSensorsActivity` + Impl**

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/ScanSensorsActivity.java`:

```java
package io.diagrid.enterprisediagnostics.activity;

import com.uber.cadence.activity.ActivityInterface;
import com.uber.cadence.activity.ActivityMethod;
import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;

@ActivityInterface
public interface ScanSensorsActivity {
    @ActivityMethod
    ScanResult execute(ScanInput input);
}
```

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/ScanSensorsActivityImpl.java`:

```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
public class ScanSensorsActivityImpl implements ScanSensorsActivity, ScanActivity {

    private static final List<String> CANDIDATES = List.of(
        "lateral sensor array calibration drift",
        "subspace gradient detector noise floor +3dB",
        "long-range sensor pallet 2 packet loss",
        "navigational deflector ghost return at 14 AU",
        "tachyon detection grid intermittent");

    @Override
    public ScanResult execute(ScanInput input) {
        return MockAnalysisGenerator.generate(input.subsystem(), CANDIDATES);
    }
}
```

- [ ] **Step 6: Create `ScanWeaponsActivity` + Impl**

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/ScanWeaponsActivity.java`:

```java
package io.diagrid.enterprisediagnostics.activity;

import com.uber.cadence.activity.ActivityInterface;
import com.uber.cadence.activity.ActivityMethod;
import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;

@ActivityInterface
public interface ScanWeaponsActivity {
    @ActivityMethod
    ScanResult execute(ScanInput input);
}
```

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/ScanWeaponsActivityImpl.java`:

```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
public class ScanWeaponsActivityImpl implements ScanWeaponsActivity, ScanActivity {

    private static final List<String> CANDIDATES = List.of(
        "forward phaser bank 2 prefire chamber overheat",
        "torpedo tube 3 magnetic seal slow to engage",
        "EPS conduit to dorsal phaser strip resistance",
        "targeting scanner azimuth offset 0.02°",
        "photon launcher injector recoil compensation lag");

    @Override
    public ScanResult execute(ScanInput input) {
        return MockAnalysisGenerator.generate(input.subsystem(), CANDIDATES);
    }
}
```

- [ ] **Step 7: Compile**

Run: `cd demos/01-cadence && ./mvnw -q compile test-compile`
Expected: BUILD SUCCESS. All 5 scan interfaces + impls + marker compile.

- [ ] **Step 8: Commit**

```bash
git add demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/Scan*.java demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/activity/ScanActivity.java
git commit -m "feat(01-cadence): add 5 scan activities with per-subsystem anomaly candidates + ScanActivity marker"
```

---

## Task 9: `DiagnosticsWorkflow` interface + impl

**Files:**
- Create: `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflow.java`
- Create: `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflowImpl.java`

No unit test here — the workflow is end-to-end-tested in Task 12 with `TestWorkflowEnvironment`. Writing the impl now lets Task 10 (`CadenceConfig`) reference the type when wiring the worker.

- [ ] **Step 1: Create the workflow interface**

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflow.java`:

```java
package io.diagrid.enterprisediagnostics.workflow;

import com.uber.cadence.workflow.WorkflowInterface;
import com.uber.cadence.workflow.WorkflowMethod;
import io.diagrid.enterprisediagnostics.model.DiagnosticsInput;
import io.diagrid.enterprisediagnostics.model.DiagnosticsOutput;

@WorkflowInterface
public interface DiagnosticsWorkflow {
    @WorkflowMethod
    DiagnosticsOutput runDiagnostics(DiagnosticsInput input);
}
```

- [ ] **Step 2: Create the workflow impl**

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflowImpl.java`:

```java
package io.diagrid.enterprisediagnostics.workflow;

import com.uber.cadence.activity.ActivityOptions;
import com.uber.cadence.common.RetryOptions;
import com.uber.cadence.workflow.Async;
import com.uber.cadence.workflow.Promise;
import com.uber.cadence.workflow.Workflow;
import io.diagrid.enterprisediagnostics.activity.*;
import io.diagrid.enterprisediagnostics.model.*;
import org.slf4j.Logger;

import java.time.Duration;
import java.util.List;

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

    private final ScanWarpCoreActivity     warpCore    = Workflow.newActivityStub(ScanWarpCoreActivity.class,    ACTIVITY_OPTIONS);
    private final ScanShieldsActivity      shields     = Workflow.newActivityStub(ScanShieldsActivity.class,     ACTIVITY_OPTIONS);
    private final ScanLifeSupportActivity  lifeSupport = Workflow.newActivityStub(ScanLifeSupportActivity.class, ACTIVITY_OPTIONS);
    private final ScanSensorsActivity      sensors     = Workflow.newActivityStub(ScanSensorsActivity.class,     ACTIVITY_OPTIONS);
    private final ScanWeaponsActivity      weapons     = Workflow.newActivityStub(ScanWeaponsActivity.class,     ACTIVITY_OPTIONS);
    private final PrioritizeIssuesActivity prioritize  = Workflow.newActivityStub(PrioritizeIssuesActivity.class, ACTIVITY_OPTIONS);
    private final NotifyBridgeActivity     notify      = Workflow.newActivityStub(NotifyBridgeActivity.class,     ACTIVITY_OPTIONS);

    @Override
    public DiagnosticsOutput runDiagnostics(DiagnosticsInput input) {
        Logger logger = Workflow.getLogger(DiagnosticsWorkflowImpl.class);
        logger.info("Starting diagnostics for {}", input.shipName());

        Promise<ScanResult> pWarp        = Async.function(warpCore::execute,    new ScanInput(input.shipName(), "warp_core"));
        Promise<ScanResult> pShields     = Async.function(shields::execute,     new ScanInput(input.shipName(), "shields"));
        Promise<ScanResult> pLifeSupport = Async.function(lifeSupport::execute, new ScanInput(input.shipName(), "life_support"));
        Promise<ScanResult> pSensors     = Async.function(sensors::execute,     new ScanInput(input.shipName(), "sensors"));
        Promise<ScanResult> pWeapons     = Async.function(weapons::execute,     new ScanInput(input.shipName(), "weapons"));

        Promise.allOf(pWarp, pShields, pLifeSupport, pSensors, pWeapons).get();

        List<ScanResult> scans = List.of(
            pWarp.get(), pShields.get(), pLifeSupport.get(), pSensors.get(), pWeapons.get());

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

- [ ] **Step 3: Compile**

Run: `cd demos/01-cadence && ./mvnw -q compile`
Expected: BUILD SUCCESS.

- [ ] **Step 4: Commit**

```bash
git add demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/workflow/
git commit -m "feat(01-cadence): add DiagnosticsWorkflow with fan-out, durable sleep, conditional bridge alert"
```

---

## Task 10: `CadenceProperties` + `application.yml`

**Files:**
- Create: `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/config/CadenceProperties.java`
- Modify: `demos/01-cadence/src/main/resources/application.yml`

- [ ] **Step 1: Create the properties record**

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/config/CadenceProperties.java`:

```java
package io.diagrid.enterprisediagnostics.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

@ConfigurationProperties(prefix = "cadence")
public record CadenceProperties(
    String serviceName,
    String host,
    int port,
    String domain,
    String taskList) {}
```

- [ ] **Step 2: Update `application.yml`**

Replace `demos/01-cadence/src/main/resources/application.yml` with:

```yaml
server:
  port: 8080

logging:
  level:
    root: INFO
    io.diagrid.enterprisediagnostics: INFO
    com.uber.cadence: WARN

cadence:
  service-name: cadence-frontend
  host: 127.0.0.1
  port: 7933
  domain: default
  task-list: diagnostics-tl
```

- [ ] **Step 3: Compile**

Run: `cd demos/01-cadence && ./mvnw -q compile`
Expected: BUILD SUCCESS.

- [ ] **Step 4: Commit**

```bash
git add demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/config/CadenceProperties.java demos/01-cadence/src/main/resources/application.yml
git commit -m "feat(01-cadence): add CadenceProperties + populated application.yml"
```

---

## Task 11: `CadenceConfig` + `DomainRegistrar`

**Files:**
- Create: `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/config/CadenceConfig.java`
- Create: `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/config/DomainRegistrar.java`
- Modify: `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/EnterpriseDiagnosticsApplication.java` (add `@EnableConfigurationProperties`)
- Modify: `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflowImpl.java` (no change to body — but Spring needs it as a bean for type registration; add `@Component`)

> **Why domain registration lives in a separate `ApplicationRunner` bean (not a `@PostConstruct` on `CadenceConfig`):** an `ApplicationRunner` runs after the Spring context is fully refreshed and after the worker has started. The domain only needs to exist before a workflow is *started* (by an external `POST /start` call), so registering it after startup is safe and avoids the `@PostConstruct` + `@Bean(workflowService)` recursion trap shown in the spec.

- [ ] **Step 1: Mark `DiagnosticsWorkflowImpl` as a Spring bean**

Modify `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflowImpl.java` — add the `@Component` annotation at the class level. The workflow body is unchanged.

```java
package io.diagrid.enterprisediagnostics.workflow;

// (existing imports)
import org.springframework.stereotype.Component;

@Component
public class DiagnosticsWorkflowImpl implements DiagnosticsWorkflow {
    // (body unchanged from Task 9)
}
```

> The `@Component` lets Spring construct one instance so `CadenceConfig` can grab `.getClass()` from it for `worker.registerWorkflowImplementationTypes(...)`. Cadence's worker ignores the bean instance and creates fresh workflow objects per execution.

- [ ] **Step 2: Create `CadenceConfig.java`**

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/config/CadenceConfig.java`:

```java
package io.diagrid.enterprisediagnostics.config;

import com.uber.cadence.client.WorkflowClient;
import com.uber.cadence.client.WorkflowClientOptions;
import com.uber.cadence.serviceclient.ClientOptions;
import com.uber.cadence.serviceclient.IWorkflowService;
import com.uber.cadence.serviceclient.WorkflowServiceTChannel;
import com.uber.cadence.worker.Worker;
import com.uber.cadence.worker.WorkerFactory;
import io.diagrid.enterprisediagnostics.activity.NotifyBridgeActivityImpl;
import io.diagrid.enterprisediagnostics.activity.PrioritizeIssuesActivityImpl;
import io.diagrid.enterprisediagnostics.activity.ScanActivity;
import io.diagrid.enterprisediagnostics.workflow.DiagnosticsWorkflowImpl;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import java.util.ArrayList;
import java.util.List;

@Configuration
public class CadenceConfig {

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
                .setDomain(props.domain())
                .build());
    }

    @Bean(destroyMethod = "shutdownNow")
    public WorkerFactory workerFactory(
            WorkflowClient client,
            DiagnosticsWorkflowImpl workflowImpl,
            List<ScanActivity> scanActivities,
            PrioritizeIssuesActivityImpl prioritize,
            NotifyBridgeActivityImpl notify) {

        WorkerFactory factory = WorkerFactory.newInstance(client);
        Worker worker = factory.newWorker(props.taskList());

        worker.registerWorkflowImplementationTypes(workflowImpl.getClass());

        List<Object> activityImpls = new ArrayList<>(scanActivities);
        activityImpls.add(prioritize);
        activityImpls.add(notify);
        worker.registerActivitiesImplementations(activityImpls.toArray());

        factory.start();
        return factory;
    }
}
```

> **Why pass `workflowImpl.getClass()` (not `DiagnosticsWorkflowImpl.class`):** functionally equivalent here, but injecting the bean documents the dependency in the bean graph; Spring will refuse to build the `WorkerFactory` if `DiagnosticsWorkflowImpl` is missing, which surfaces wiring mistakes early.

- [ ] **Step 3: Create `DomainRegistrar.java`**

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/config/DomainRegistrar.java`:

```java
package io.diagrid.enterprisediagnostics.config;

import com.uber.cadence.DomainAlreadyExistsError;
import com.uber.cadence.RegisterDomainRequest;
import com.uber.cadence.serviceclient.IWorkflowService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.ApplicationArguments;
import org.springframework.boot.ApplicationRunner;
import org.springframework.stereotype.Component;

@Component
public class DomainRegistrar implements ApplicationRunner {

    private static final Logger log = LoggerFactory.getLogger(DomainRegistrar.class);

    private final IWorkflowService service;
    private final CadenceProperties props;

    public DomainRegistrar(IWorkflowService service, CadenceProperties props) {
        this.service = service;
        this.props = props;
    }

    @Override
    public void run(ApplicationArguments args) {
        RegisterDomainRequest req = new RegisterDomainRequest()
            .setName(props.domain())
            .setWorkflowExecutionRetentionPeriodInDays(1);
        try {
            service.RegisterDomain(req);
            log.info("registered Cadence domain '{}'", props.domain());
        } catch (DomainAlreadyExistsError ignored) {
            log.info("Cadence domain '{}' already exists", props.domain());
        } catch (Exception e) {
            log.error("failed to register Cadence domain '{}'", props.domain(), e);
            throw new IllegalStateException("Cadence domain registration failed", e);
        }
    }
}
```

- [ ] **Step 4: Enable `CadenceProperties` in the application class**

Modify `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/EnterpriseDiagnosticsApplication.java`:

```java
package io.diagrid.enterprisediagnostics;

import io.diagrid.enterprisediagnostics.config.CadenceProperties;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.context.properties.EnableConfigurationProperties;

@SpringBootApplication
@EnableConfigurationProperties(CadenceProperties.class)
public class EnterpriseDiagnosticsApplication {
    public static void main(String[] args) {
        SpringApplication.run(EnterpriseDiagnosticsApplication.class, args);
    }
}
```

- [ ] **Step 5: Verify the cluster is up**

```bash
docker ps --filter name=cadence-server --format '{{.Status}}' | grep -q Up && echo "cadence up" || (cd demos/01-cadence && docker compose up -d && sleep 60)
```

- [ ] **Step 6: Start the Spring Boot app and verify domain registration**

In a foreground terminal:
```bash
cd demos/01-cadence && ./mvnw spring-boot:run
```

Watch the logs for:
- `registered Cadence domain 'default'` (first run) **or** `Cadence domain 'default' already exists` (subsequent runs).
- `WorkflowPoller for task list diagnostics-tl started` (or similar Cadence-client log indicating the worker is polling).
- No stack traces.

Then in another terminal:
```bash
curl -fsS http://localhost:8080/actuator 2>&1 | head -3 || echo "no actuator — expected"
```

(We didn't add actuator; we just want the embedded Tomcat to be up on 8080. The next task adds the REST controller.)

Stop the app with Ctrl+C.

- [ ] **Step 7: Verify the domain exists in Cadence Web**

Open `http://localhost:8088`. In the domain search bar, enter `default`. Expected: `default` domain listed with retention period 1 day.

- [ ] **Step 8: Commit**

```bash
git add demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/config/CadenceConfig.java demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/config/DomainRegistrar.java demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/EnterpriseDiagnosticsApplication.java demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflowImpl.java
git commit -m "feat(01-cadence): add CadenceConfig (manual @Beans) + DomainRegistrar (ApplicationRunner)"
```

---

## Task 12: `DiagnosticsWorkflowImpl` integration test via `TestWorkflowEnvironment`

**Files:**
- Modify: `demos/01-cadence/pom.xml` — add `cadence-client` `:tests` classifier dependency for `TestWorkflowEnvironment`.
- Create: `demos/01-cadence/src/test/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflowImplTest.java`

> The Cadence Java client publishes a `tests` classifier artifact containing `TestWorkflowEnvironment`. If your local resolution of 3.13.x doesn't include it, fall back to `cadence-test`/`cadence-client-tests` per the artifact list on Maven Central — pin pre-talk. Verify pre-talk: `mvn dependency:resolve -Dclassifier=tests` in `demos/01-cadence/`.

- [ ] **Step 1: Add the test dependency**

Modify `demos/01-cadence/pom.xml` — add inside `<dependencies>`:

```xml
<dependency>
    <groupId>com.uber.cadence</groupId>
    <artifactId>cadence-client</artifactId>
    <version>${cadence.version}</version>
    <classifier>tests</classifier>
    <scope>test</scope>
</dependency>
```

- [ ] **Step 2: Write the failing test**

`demos/01-cadence/src/test/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflowImplTest.java`:

```java
package io.diagrid.enterprisediagnostics.workflow;

import com.uber.cadence.client.WorkflowClient;
import com.uber.cadence.client.WorkflowOptions;
import com.uber.cadence.testing.TestEnvironmentOptions;
import com.uber.cadence.testing.TestWorkflowEnvironment;
import com.uber.cadence.worker.Worker;
import io.diagrid.enterprisediagnostics.activity.*;
import io.diagrid.enterprisediagnostics.model.*;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.RepeatedTest;
import org.junit.jupiter.api.Test;

import java.time.Duration;

import static org.junit.jupiter.api.Assertions.*;

class DiagnosticsWorkflowImplTest {

    private static final String DOMAIN = "test-domain";
    private static final String TASK_LIST = "diagnostics-tl-test";

    private TestWorkflowEnvironment env;
    private WorkflowClient client;

    @BeforeEach
    void setUp() {
        env = TestWorkflowEnvironment.newInstance(
            new TestEnvironmentOptions.Builder().setDomain(DOMAIN).build());

        Worker worker = env.newWorker(TASK_LIST);
        worker.registerWorkflowImplementationTypes(DiagnosticsWorkflowImpl.class);
        worker.registerActivitiesImplementations(
            new ScanWarpCoreActivityImpl(),
            new ScanShieldsActivityImpl(),
            new ScanLifeSupportActivityImpl(),
            new ScanSensorsActivityImpl(),
            new ScanWeaponsActivityImpl(),
            new PrioritizeIssuesActivityImpl(),
            new NotifyBridgeActivityImpl());

        env.start();
        client = env.newWorkflowClient();
    }

    @AfterEach
    void tearDown() {
        env.close();
    }

    @RepeatedTest(10)
    void runsHappyPathToRedAlertWithBridgeNotification() {
        DiagnosticsWorkflow workflow = client.newWorkflowStub(
            DiagnosticsWorkflow.class,
            new WorkflowOptions.Builder()
                .setTaskList(TASK_LIST)
                .setWorkflowId("test-" + System.nanoTime())
                .setExecutionStartToCloseTimeout(Duration.ofMinutes(5))
                .build());

        DiagnosticsOutput out = workflow.runDiagnostics(new DiagnosticsInput(
            "test-1", "USS Enterprise NCC-1701-D", "2370-04-08", "Geordi La Forge"));

        // Rigging guarantee: min(health) is very likely < 50, so severity == RED_ALERT
        // (the @RepeatedTest(10) ensures we'd notice if probabilities shifted).
        assertEquals(Severity.RED_ALERT, out.priority().severity());
        assertNotNull(out.bridgeNotification(), "bridge alert should fire on red_alert");
        assertTrue(out.bridgeNotification().message().contains("🚨 BRIDGE ALERT"),
            "got: " + out.bridgeNotification().message());
        assertTrue(out.bridgeNotification().message().contains("RED_ALERT"));
        assertEquals("USS Enterprise NCC-1701-D", out.shipName());
        assertEquals("Geordi La Forge", out.engineerName());
        assertEquals("2370-04-08", out.diagnosticsDate());
    }

    @Test
    void durableSleepIsSkippedByTestClockSoTestsAreFast() {
        long start = System.currentTimeMillis();

        DiagnosticsWorkflow workflow = client.newWorkflowStub(
            DiagnosticsWorkflow.class,
            new WorkflowOptions.Builder()
                .setTaskList(TASK_LIST)
                .setWorkflowId("test-fast-" + System.nanoTime())
                .setExecutionStartToCloseTimeout(Duration.ofMinutes(5))
                .build());

        workflow.runDiagnostics(new DiagnosticsInput(
            "test-2", "USS Voyager", "2371-01-01", "B'Elanna Torres"));

        long elapsed = System.currentTimeMillis() - start;
        assertTrue(elapsed < 30_000,
            "TestWorkflowEnvironment should skip the 60s real-clock sleep; took " + elapsed + " ms");
    }
}
```

- [ ] **Step 3: Run the test to verify it passes**

Run: `cd demos/01-cadence && ./mvnw -q -Dtest=DiagnosticsWorkflowImplTest test`
Expected: PASS (10 repeats of the happy path + the fast-clock check = 11 total).

> If the test hangs at >30s, check that `TestWorkflowEnvironment` is actually skipping the timer (this is its default behavior). If it's not, verify the `cadence-client:tests` artifact resolved correctly — `mvn dependency:tree | grep cadence` should show it on the test classpath.

- [ ] **Step 4: Commit**

```bash
git add demos/01-cadence/pom.xml demos/01-cadence/src/test/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflowImplTest.java
git commit -m "test(01-cadence): add TestWorkflowEnvironment integration test for happy path + clock skip"
```

---

## Task 13: `DiagnosticsController` REST API + `.http` file

**Files:**
- Create: `demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/api/DiagnosticsController.java`
- Create: `demos/01-cadence/enterprise-diagnostics.http`

- [ ] **Step 1: Create `DiagnosticsController.java`**

`demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/api/DiagnosticsController.java`:

```java
package io.diagrid.enterprisediagnostics.api;

import com.uber.cadence.client.WorkflowClient;
import com.uber.cadence.client.WorkflowOptions;
import com.uber.cadence.client.WorkflowStub;
import io.diagrid.enterprisediagnostics.config.CadenceProperties;
import io.diagrid.enterprisediagnostics.model.DiagnosticsInput;
import io.diagrid.enterprisediagnostics.model.DiagnosticsOutput;
import io.diagrid.enterprisediagnostics.workflow.DiagnosticsWorkflow;
import org.springframework.web.bind.annotation.*;

import java.time.Duration;
import java.util.Map;
import java.util.Optional;

@RestController
public class DiagnosticsController {

    private final WorkflowClient client;
    private final CadenceProperties props;

    public DiagnosticsController(WorkflowClient client, CadenceProperties props) {
        this.client = client;
        this.props = props;
    }

    @PostMapping("/start")
    public Map<String, String> start(@RequestBody DiagnosticsInput input) {
        DiagnosticsWorkflow stub = client.newWorkflowStub(
            DiagnosticsWorkflow.class,
            new WorkflowOptions.Builder()
                .setTaskList(props.taskList())
                .setWorkflowId(input.id())
                .setExecutionStartToCloseTimeout(Duration.ofMinutes(10))
                .build());

        WorkflowClient.start(stub::runDiagnostics, input);
        return Map.of("workflowId", input.id());
    }

    @GetMapping("/status/{id}")
    public Map<String, Object> status(@PathVariable String id) {
        WorkflowStub stub = client.newUntypedWorkflowStub(id, Optional.empty(), Optional.empty());
        try {
            DiagnosticsOutput result = stub.getResult(Duration.ofMillis(500), DiagnosticsOutput.class);
            return Map.of("status", "COMPLETED", "output", result);
        } catch (Exception e) {
            return Map.of("status", "RUNNING_OR_FAILED", "detail", e.getClass().getSimpleName());
        }
    }

    @PostMapping("/terminate/{id}")
    public Map<String, String> terminate(@PathVariable String id) {
        WorkflowStub stub = client.newUntypedWorkflowStub(id, Optional.empty(), Optional.empty());
        stub.terminate("manual", "manual termination via /terminate endpoint");
        return Map.of("workflowId", id, "terminated", "true");
    }

    @PostMapping("/cancel/{id}")
    public Map<String, String> cancel(@PathVariable String id) {
        WorkflowStub stub = client.newUntypedWorkflowStub(id, Optional.empty(), Optional.empty());
        stub.cancel();
        return Map.of("workflowId", id, "cancelled", "true");
    }
}
```

> **Status endpoint note:** `stub.getResult(timeout, type)` blocks up to the timeout waiting for completion. With 500 ms, an in-progress workflow throws and we surface `RUNNING_OR_FAILED`. That's good enough for the talk — the real UI is Cadence Web. Don't gold-plate a full state machine for an on-stage demo.

- [ ] **Step 2: Create `enterprise-diagnostics.http`**

`demos/01-cadence/enterprise-diagnostics.http`:

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

### Terminate
POST http://localhost:8080/terminate/enterprise-ncc-1701

### Cancel
POST http://localhost:8080/cancel/enterprise-ncc-1701
```

- [ ] **Step 3: Verify the project builds and tests pass**

Run: `cd demos/01-cadence && ./mvnw -q test`
Expected: BUILD SUCCESS, all tests green (Severity 6, MockAnalysisGenerator 5×50, Prioritize 6, NotifyBridge 2, Workflow 10+1).

- [ ] **Step 4: Commit**

```bash
git add demos/01-cadence/src/main/java/io/diagrid/enterprisediagnostics/api/DiagnosticsController.java demos/01-cadence/enterprise-diagnostics.http
git commit -m "feat(01-cadence): add REST controller (POST /start, GET /status, terminate, cancel) + .http file"
```

---

## Task 14: End-to-end verification against the real Cadence cluster

This task does not write code — it executes the demo to verify the kill-and-resume choreography works as the spec promises.

- [ ] **Step 1: Ensure the cluster is up**

```bash
cd demos/01-cadence && docker compose up -d
until docker inspect --format='{{.State.Health.Status}}' cadence-cassandra | grep -q healthy; do sleep 5; done
echo "cassandra healthy"
```

- [ ] **Step 2: Start the Spring Boot app**

In Terminal A:
```bash
cd demos/01-cadence && ./mvnw spring-boot:run
```

Watch for:
- `registered Cadence domain 'default'` (or "already exists")
- worker startup log lines from `com.uber.cadence`
- `Started EnterpriseDiagnosticsApplication`

- [ ] **Step 3: Start a workflow**

In Terminal B:
```bash
curl -s -X POST http://localhost:8080/start \
  -H 'Content-Type: application/json' \
  -d '{"id":"enterprise-ncc-1701","shipName":"USS Enterprise NCC-1701-D","diagnosticsDate":"2370-04-08","engineerName":"Geordi La Forge"}'
```
Expected: `{"workflowId":"enterprise-ncc-1701"}`.

- [ ] **Step 4: Watch the 5 scan activities complete in Cadence Web**

Open `http://localhost:8088` → `default` domain → workflow id `enterprise-ncc-1701`. Within 2–3 seconds, the history should show 5 `ActivityTaskScheduled` + 5 `ActivityTaskStarted` + 5 `ActivityTaskCompleted` events, then a `TimerStarted` (60s) event.

- [ ] **Step 5: Kill the Spring Boot app ~20 seconds into the timer**

In Terminal A, press `Ctrl+C` when the Cadence Web history shows `TimerStarted` (within ~20s of starting the workflow).

Expected: Spring Boot exits. The Cadence server is still running; the timer is still pending in `default`/`enterprise-ncc-1701`.

- [ ] **Step 6: Confirm the workflow is frozen on the timer**

Refresh Cadence Web. The workflow `enterprise-ncc-1701` should remain `Running` with the last event being `TimerStarted`.

- [ ] **Step 7: Restart the Spring Boot app and watch the workflow resume**

In Terminal A:
```bash
cd demos/01-cadence && ./mvnw spring-boot:run
```

When the 60-second timer fires (the timer keeps ticking even while the worker is offline), the new worker picks up the next decision task. Cadence Web should show, in order:
1. `TimerFired`
2. `ActivityTaskScheduled` / `Started` / `Completed` for `PrioritizeIssuesActivity`
3. `ActivityTaskScheduled` / `Started` / `Completed` for `NotifyBridgeActivity` (if severity = `red_alert`)
4. `WorkflowExecutionCompleted`

In Terminal A's stdout: `🚨 BRIDGE ALERT — USS USS Enterprise NCC-1701-D — RED_ALERT: ...` at WARN level.

- [ ] **Step 8: Fetch the final status**

In Terminal B:
```bash
curl -s http://localhost:8080/status/enterprise-ncc-1701 | jq .
```
Expected: `status: "COMPLETED"`, `output.priority.severity: "red_alert"`, `output.bridgeNotification.message` containing the banner.

- [ ] **Step 9: Run it 10 times to confirm the rigging holds**

```bash
for i in 1 2 3 4 5 6 7 8 9 10; do
  ID="enterprise-rerun-$i"
  curl -s -X POST http://localhost:8080/start -H 'Content-Type: application/json' \
    -d "{\"id\":\"$ID\",\"shipName\":\"USS Enterprise NCC-1701-D\",\"diagnosticsDate\":\"2370-04-08\",\"engineerName\":\"Geordi\"}" > /dev/null
done

# Wait > 90 s, then poll all 10
sleep 90
for i in 1 2 3 4 5 6 7 8 9 10; do
  echo -n "rerun-$i: "
  curl -s http://localhost:8080/status/enterprise-rerun-$i | jq -r '.output.priority.severity // .status'
done
```
Expected: all 10 print `red_alert` (the rigging should hold; if any are `yellow_alert`/`green` the biased range is wrong).

- [ ] **Step 10: Stop the app and the cluster**

Terminal A: Ctrl+C.

```bash
cd demos/01-cadence && docker compose down
```

(Keep the Cassandra volume by default — restarting `docker compose up -d` keeps the `default` domain registered for the next dry run.)

- [ ] **Step 11: Note any deviations**

If any of the verification steps failed, fix the underlying code and re-run from step 2. Do not commit a half-working demo.

---

## Task 15: README + final commit

**Files:**
- Create: `demos/01-cadence/README.md`

- [ ] **Step 1: Create `README.md`**

`demos/01-cadence/README.md`:

````markdown
# Cadence (Java) — USS Enterprise diagnostics

One of five framework demos for the *Durable Execution Battle* talk. Cadence is the ancestor; Temporal forked from it. This demo's purpose on stage is to show the lineage — same SDK surface (`@WorkflowMethod`, `Async.function`, `Promise.allOf`, `Workflow.sleep`) and the cost-of-entry delta (manual `@Bean` wiring + Cassandra-backed cluster) vs Temporal's `start-dev` + Spring Boot starter.

## Stack

- Java 21 · Maven · Spring Boot 3.4.x
- `com.uber.cadence:cadence-client:3.13.x`
- Cadence cluster via `docker-compose.yml` (Cassandra 4 + `ubercadence/server` + `ubercadence/web`)

## Prerequisites

- Java 21 (verify with `java -version`)
- Docker + Docker Compose
- Approximately 2 GB free RAM for Cassandra
- Optional: `jq` for pretty-printing status responses

## One-time pre-talk setup

```bash
docker pull cassandra:4.1
docker pull ubercadence/server:1.2.13
docker pull ubercadence/web:4.1.4
./mvnw dependency:go-offline
```

## Run the demo

In three terminals:

```bash
# Terminal 1 — cluster
docker compose up

# Terminal 2 — Spring Boot app (worker + REST API on :8080)
./mvnw spring-boot:run

# Terminal 3 — start the workflow
curl -X POST http://localhost:8080/start \
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

Watch Cadence Web at `http://localhost:8088` → `default` domain → workflow `enterprise-ncc-1701`.

## Kill-and-resume choreography

1. Trigger `/start` — 5 scan activities complete in ~2 s.
2. Workflow enters a 60-second durable timer (the kill point).
3. **At T+20 s** (during the timer): Ctrl+C the Spring Boot app in Terminal 2.
4. Cadence Web shows the workflow frozen on `TimerStarted`. The Cadence server still holds the timer.
5. Restart `./mvnw spring-boot:run`.
6. When the 60-second timer fires, the new worker picks up the decision task. `PrioritizeIssuesActivity` runs; if severity is `yellow_alert`/`red_alert`, `NotifyBridgeActivity` fires.
7. A `🚨 BRIDGE ALERT` line appears in Terminal 2's stdout (WARN level).

## API

| Method | Path | Purpose |
|---|---|---|
| `POST` | `/start` | Start a workflow with the given input. Returns `{ workflowId }`. |
| `GET` | `/status/{id}` | If completed, returns the `DiagnosticsOutput`. Otherwise reports `RUNNING_OR_FAILED`. The real UI is Cadence Web. |
| `POST` | `/terminate/{id}` | Force-terminate the workflow. |
| `POST` | `/cancel/{id}` | Request graceful cancellation. (Cadence has no first-class pause/resume.) |

## Tests

```bash
./mvnw test
```

Runs unit tests for `Severity` Jackson serialization, `MockAnalysisGenerator` invariants, `PrioritizeIssuesActivity` severity thresholds, and `NotifyBridgeActivity` message format, plus an in-memory workflow test via `TestWorkflowEnvironment` (the durable 60-s sleep is skipped by the test clock, so the suite runs in seconds).

## Comparison to the sister Temporal demo

The companion demo at [`../02-temporal/`](../02-temporal/) is the *fork*. Compare side-by-side:

- `workflow/DiagnosticsWorkflowImpl.java` — body is byte-for-byte identical; the diff is `com.uber.cadence.*` vs `io.temporal.*` imports, `new ActivityOptions.Builder()` vs `ActivityOptions.newBuilder()`, an extra `setScheduleToCloseTimeout` (required by Cadence), and the missing `@WorkflowImpl` annotation on the Cadence side.
- `config/CadenceConfig.java` — exists only in this demo. The Temporal Spring Boot starter handles the equivalent wiring.

## Talking points on stage

- *Cadence is from 2017; Temporal's Spring Boot starter is from 2022. One config file is the five years of ergonomics work.*
- *Domain* (Cadence) ↔ *namespace* (Temporal). *Task list* (Cadence) ↔ *task queue* (Temporal). The renaming happened at the fork.
- The kill-and-resume payoff is identical — the *durable execution* primitive is what both inherit from the same lineage.

## Pin / verify before the talk

- `cassandra:4.1`, `ubercadence/server:1.2.13`, `ubercadence/web:4.1.4`: pull and verify pre-talk; never `:latest`.
- `cadence-client` version pinned in `pom.xml` (`3.13.0` at time of writing).
- Run `./mvnw dependency:go-offline` so the Spring Boot startup doesn't pull from Maven Central on stage.
- Do a full end-to-end dry run including the kill-and-resume sequence on the talk laptop.
````

- [ ] **Step 2: Run the full test suite one more time**

Run: `cd demos/01-cadence && ./mvnw test`
Expected: all green.

- [ ] **Step 3: Commit**

```bash
git add demos/01-cadence/README.md
git commit -m "docs(01-cadence): add README with run instructions + kill-resume choreography"
```

- [ ] **Step 4: Final verification — repo state**

```bash
git status
git log --oneline -20
```
Expected: clean working tree, ~13 commits added to the branch describing the build sequence.

---

## Done criteria

- [ ] All 15 tasks above complete with their commits.
- [ ] `./mvnw test` passes from a clean checkout.
- [ ] The Task 14 end-to-end choreography succeeds twice in a row.
- [ ] `DiagnosticsWorkflowImpl.java` in `demos/01-cadence/` and `demos/02-temporal/` diff to imports + `ActivityOptions` builder shape + the `@WorkflowImpl` annotation only.
- [ ] The model records, `MockAnalysisGenerator.java`, the `PrioritizeIssuesActivityImpl.execute` body, and the `NotifyBridgeActivityImpl.execute` body are byte-for-byte identical between `01-cadence/` and `02-temporal/`.
- [ ] The `🚨 BRIDGE ALERT` line at WARN level in worker stdout matches the canonical format exactly.

---

## Notes for executors

- **Don't touch `plan.md` or the spec.** Both have been frozen; this plan is the implementation contract. If you find a contradiction, ask before changing either document.
- **Don't add Lombok, MapStruct, or other code generators.** The demo is meant to be readable on a projector — every line on screen should be Java you can point at.
- **Don't introduce a fluent assertion library** (AssertJ, Truth). JUnit 5 assertions are enough for these unit tests and keep the dependency tree small.
- **Don't add Spring Boot Actuator.** Not needed for the demo. The real observability is Cadence Web.
- **Don't add a Dockerfile for the Spring Boot app.** The demo runs `./mvnw spring-boot:run` from the laptop — that's the point of the comparison with `start-dev`-style ergonomics.
- **If `cadence-client:3.13.0` resolves differently or `WorkflowServiceTChannel` API has moved** in the version you actually pin, adjust `CadenceConfig` and note the change in the README's "Pin / verify before the talk" section. Don't downgrade silently.
