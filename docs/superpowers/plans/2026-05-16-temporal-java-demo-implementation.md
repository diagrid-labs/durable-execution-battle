# Temporal (Java) Demo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the USS Enterprise diagnostics workflow demo in Temporal Java (Spring Boot 3, Java 21, Maven) under `demos/02-temporal/`, matching the canonical scenario and cross-demo invariants of the .NET reference at `demos/04-dapr/`.

**Architecture:** Single Maven module hosting one Spring Boot process that contains both the Temporal worker (auto-discovered via `temporal-spring-boot-starter-alpha`) and a REST controller. One `@WorkflowInterface` (`DiagnosticsWorkflow`) drives 5 parallel scan activities, a 60-second durable timer (the on-stage kill point), a deterministic prioritizer, and a conditional bridge notification. State lives in `temporal server start-dev` (single binary, no Docker). Java records + Jackson handle the wire format; the `Severity` enum uses `@JsonProperty` to match the .NET strings (`green` / `yellow_alert` / `red_alert`).

**Tech Stack:** Java 21 LTS · Maven (with wrapper) · Spring Boot 3.4.x · `io.temporal:temporal-sdk` + `temporal-spring-boot-starter-alpha` 1.30.x · Jackson (default Spring Boot) · JUnit 5 + `io.temporal:temporal-testing` · Temporal CLI dev server.

**Reference (scenario only, not style):** `demos/04-dapr/EnterpriseDiagnostics.ApiService/`. Activity names, severity strings, subsystem ids, health range, retry policy shape, bridge message format, and workflow id must match. Do **not** transliterate C# style — write idiomatic Java.

---

## File Structure

```
demos/02-temporal/
├── README.md
├── pom.xml
├── mvnw, mvnw.cmd, .mvn/wrapper/...
├── enterprise-diagnostics.http
└── src/
    ├── main/
    │   ├── java/io/diagrid/enterprisediagnostics/
    │   │   ├── EnterpriseDiagnosticsApplication.java
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
        ├── model/ModelSerializationTest.java
        ├── activity/MockAnalysisGeneratorTest.java
        ├── activity/PrioritizeIssuesActivityImplTest.java
        ├── activity/NotifyBridgeActivityImplTest.java
        └── workflow/DiagnosticsWorkflowImplTest.java
```

Single responsibility per file. Activity interface and impl are split per Java convention. Tests live mirror to the source package.

---

## Task 1: Maven + Spring Boot skeleton

**Files:**
- Create: `demos/02-temporal/pom.xml`
- Create: `demos/02-temporal/mvnw`, `demos/02-temporal/mvnw.cmd`, `demos/02-temporal/.mvn/wrapper/maven-wrapper.properties`
- Create: `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/EnterpriseDiagnosticsApplication.java`
- Create: `demos/02-temporal/src/main/resources/application.yml` (placeholder — final values in Task 10)
- Create: `demos/02-temporal/.gitignore`

- [ ] **Step 1: Create `pom.xml`**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<project xmlns="http://maven.apache.org/POM/4.0.0"
         xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xsi:schemaLocation="http://maven.apache.org/POM/4.0.0
             https://maven.apache.org/xsd/maven-4.0.0.xsd">
    <modelVersion>4.0.0</modelVersion>

    <parent>
        <groupId>org.springframework.boot</groupId>
        <artifactId>spring-boot-starter-parent</artifactId>
        <version>3.4.1</version>
        <relativePath/>
    </parent>

    <groupId>io.diagrid</groupId>
    <artifactId>enterprise-diagnostics</artifactId>
    <version>0.1.0-SNAPSHOT</version>
    <name>enterprise-diagnostics</name>
    <description>Temporal Java demo for the Durable Execution Battle talk</description>

    <properties>
        <java.version>21</java.version>
        <temporal.version>1.30.1</temporal.version>
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

        <dependency>
            <groupId>org.springframework.boot</groupId>
            <artifactId>spring-boot-starter-test</artifactId>
            <scope>test</scope>
        </dependency>
        <dependency>
            <groupId>io.temporal</groupId>
            <artifactId>temporal-testing</artifactId>
            <version>${temporal.version}</version>
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

- [ ] **Step 2: Add Maven wrapper**

Run from `demos/02-temporal/`:

```bash
mvn -N wrapper:wrapper -Dmaven=3.9.9
```

Expected: `mvnw`, `mvnw.cmd`, `.mvn/wrapper/maven-wrapper.properties` created. If `mvn` is not installed system-wide, copy the wrapper files from any existing Spring Boot project on the machine; or use `curl -o mvnw https://raw.githubusercontent.com/takari/maven-wrapper/master/mvnw` etc. The wrapper version is not load-bearing; verify Step 4 still works.

- [ ] **Step 3: Create `EnterpriseDiagnosticsApplication.java`**

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

```yaml
server:
  port: 8080

logging:
  level:
    io.diagrid.enterprisediagnostics: INFO
    io.temporal: INFO
```

(Temporal configuration is added in Task 10. The app at this point does not connect to Temporal and that is OK — the Temporal starter only activates when its configuration block is present.)

- [ ] **Step 5: Create `.gitignore`**

```
target/
.idea/
*.iml
.vscode/
.mvn/wrapper/maven-wrapper.jar
HELP.md
```

- [ ] **Step 6: Verify the skeleton compiles and starts**

```bash
cd demos/02-temporal && ./mvnw -q clean compile
```

Expected: BUILD SUCCESS, no compilation errors.

```bash
cd demos/02-temporal && ./mvnw -q spring-boot:run &
sleep 8
curl -sf http://localhost:8080/actuator/health || curl -sf http://localhost:8080/ -o /dev/null -w '%{http_code}\n'
kill %1 2>/dev/null
```

Expected: process starts, `curl` returns `404` (no controller yet, but the server is up). If you see `Connection refused`, the app didn't start — read the log.

- [ ] **Step 7: Commit**

```bash
git add demos/02-temporal/pom.xml demos/02-temporal/mvnw demos/02-temporal/mvnw.cmd \
        demos/02-temporal/.mvn demos/02-temporal/.gitignore \
        demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/EnterpriseDiagnosticsApplication.java \
        demos/02-temporal/src/main/resources/application.yml
git commit -m "feat(02-temporal): scaffold Maven + Spring Boot 3 skeleton"
```

---

## Task 2: `Severity` enum + Jackson round-trip test

The `Severity` enum is the highest-risk model element because the wire strings (`green` / `yellow_alert` / `red_alert`) must exactly match the .NET reference. Write the test first.

**Files:**
- Create: `demos/02-temporal/src/test/java/io/diagrid/enterprisediagnostics/model/SeverityJsonTest.java`
- Create: `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/model/Severity.java`

- [ ] **Step 1: Write the failing test**

```java
package io.diagrid.enterprisediagnostics.model;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.Test;

import static org.assertj.core.api.Assertions.assertThat;

class SeverityJsonTest {

    private final ObjectMapper mapper = new ObjectMapper();

    @Test
    void greenSerializesToLowercase() throws Exception {
        assertThat(mapper.writeValueAsString(Severity.GREEN)).isEqualTo("\"green\"");
    }

    @Test
    void yellowAlertSerializesToSnakeCase() throws Exception {
        assertThat(mapper.writeValueAsString(Severity.YELLOW_ALERT)).isEqualTo("\"yellow_alert\"");
    }

    @Test
    void redAlertSerializesToSnakeCase() throws Exception {
        assertThat(mapper.writeValueAsString(Severity.RED_ALERT)).isEqualTo("\"red_alert\"");
    }

    @Test
    void redAlertDeserializesFromSnakeCase() throws Exception {
        assertThat(mapper.readValue("\"red_alert\"", Severity.class)).isEqualTo(Severity.RED_ALERT);
    }

    @Test
    void yellowAlertDeserializesFromSnakeCase() throws Exception {
        assertThat(mapper.readValue("\"yellow_alert\"", Severity.class)).isEqualTo(Severity.YELLOW_ALERT);
    }
}
```

- [ ] **Step 2: Run the test and verify it fails**

```bash
cd demos/02-temporal && ./mvnw -q test -Dtest=SeverityJsonTest
```

Expected: FAIL — compilation error, `Severity` class does not exist.

- [ ] **Step 3: Create `Severity.java` with `@JsonProperty` on each constant**

```java
package io.diagrid.enterprisediagnostics.model;

import com.fasterxml.jackson.annotation.JsonProperty;

public enum Severity {
    @JsonProperty("green")        GREEN,
    @JsonProperty("yellow_alert") YELLOW_ALERT,
    @JsonProperty("red_alert")    RED_ALERT
}
```

- [ ] **Step 4: Run the test and verify it passes**

```bash
cd demos/02-temporal && ./mvnw -q test -Dtest=SeverityJsonTest
```

Expected: PASS, 5 tests run.

- [ ] **Step 5: Commit**

```bash
git add demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/model/Severity.java \
        demos/02-temporal/src/test/java/io/diagrid/enterprisediagnostics/model/SeverityJsonTest.java
git commit -m "feat(02-temporal): add Severity enum with snake_case JSON wire format"
```

---

## Task 3: All model records + serialization smoke test

**Files:**
- Create: `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/model/DiagnosticsInput.java`
- Create: `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/model/DiagnosticsOutput.java`
- Create: `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/model/ScanInput.java`
- Create: `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/model/ScanResult.java`
- Create: `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/model/PrioritizeInput.java`
- Create: `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/model/Priority.java`
- Create: `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/model/BridgeNotificationInput.java`
- Create: `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/model/BridgeNotificationResult.java`
- Create: `demos/02-temporal/src/test/java/io/diagrid/enterprisediagnostics/model/ModelSerializationTest.java`

- [ ] **Step 1: Write the failing test**

```java
package io.diagrid.enterprisediagnostics.model;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;

class ModelSerializationTest {

    private final ObjectMapper mapper = new ObjectMapper();

    @Test
    void diagnosticsInputRoundTrips() throws Exception {
        var input = new DiagnosticsInput(
            "enterprise-ncc-1701",
            "USS Enterprise NCC-1701-D",
            "2370-04-08",
            "Geordi La Forge");

        String json = mapper.writeValueAsString(input);
        assertThat(json).contains("\"id\":\"enterprise-ncc-1701\"");
        assertThat(json).contains("\"shipName\":\"USS Enterprise NCC-1701-D\"");

        DiagnosticsInput round = mapper.readValue(json, DiagnosticsInput.class);
        assertThat(round).isEqualTo(input);
    }

    @Test
    void scanResultRoundTrips() throws Exception {
        var result = new ScanResult("warp_core", 42, List.of("plasma conduit micro-fracture"));
        String json = mapper.writeValueAsString(result);

        assertThat(json).contains("\"subsystem\":\"warp_core\"");
        assertThat(json).contains("\"health\":42");
        assertThat(json).contains("\"anomalies\":[\"plasma conduit micro-fracture\"]");

        assertThat(mapper.readValue(json, ScanResult.class)).isEqualTo(result);
    }

    @Test
    void diagnosticsOutputAllowsNullBridgeNotification() throws Exception {
        var priority = new Priority(Severity.GREEN, "all nominal");
        var output = new DiagnosticsOutput(
            "USS Enterprise NCC-1701-D",
            "2370-04-08",
            "Geordi La Forge",
            priority,
            null);

        String json = mapper.writeValueAsString(output);
        assertThat(json).contains("\"bridgeNotification\":null");

        DiagnosticsOutput round = mapper.readValue(json, DiagnosticsOutput.class);
        assertThat(round.bridgeNotification()).isNull();
        assertThat(round.priority().severity()).isEqualTo(Severity.GREEN);
    }

    @Test
    void priorityUsesSnakeCaseSeverity() throws Exception {
        String json = mapper.writeValueAsString(new Priority(Severity.RED_ALERT, "critical"));
        assertThat(json).contains("\"severity\":\"red_alert\"");
        assertThat(json).contains("\"summary\":\"critical\"");
    }
}
```

- [ ] **Step 2: Run the test and verify it fails**

```bash
cd demos/02-temporal && ./mvnw -q test -Dtest=ModelSerializationTest
```

Expected: FAIL — compilation errors, `DiagnosticsInput` etc. do not exist.

- [ ] **Step 3: Create the records**

`DiagnosticsInput.java`:
```java
package io.diagrid.enterprisediagnostics.model;

public record DiagnosticsInput(
    String id,
    String shipName,
    String diagnosticsDate,
    String engineerName) {}
```

`DiagnosticsOutput.java`:
```java
package io.diagrid.enterprisediagnostics.model;

public record DiagnosticsOutput(
    String shipName,
    String diagnosticsDate,
    String engineerName,
    Priority priority,
    BridgeNotificationResult bridgeNotification) {}
```

`ScanInput.java`:
```java
package io.diagrid.enterprisediagnostics.model;

public record ScanInput(String shipName, String subsystem) {}
```

`ScanResult.java`:
```java
package io.diagrid.enterprisediagnostics.model;

import java.util.List;

public record ScanResult(String subsystem, int health, List<String> anomalies) {}
```

`PrioritizeInput.java`:
```java
package io.diagrid.enterprisediagnostics.model;

import java.util.List;

public record PrioritizeInput(String shipName, List<ScanResult> scans) {}
```

`Priority.java`:
```java
package io.diagrid.enterprisediagnostics.model;

public record Priority(Severity severity, String summary) {}
```

`BridgeNotificationInput.java`:
```java
package io.diagrid.enterprisediagnostics.model;

public record BridgeNotificationInput(String shipName, String engineerName, Priority priority) {}
```

`BridgeNotificationResult.java`:
```java
package io.diagrid.enterprisediagnostics.model;

public record BridgeNotificationResult(String message) {}
```

Note: in the test, `output.bridgeNotification()` returns `BridgeNotificationResult`, not `String`. Update the test if you copied it before this clarification.

- [ ] **Step 4: Run the test and verify it passes**

```bash
cd demos/02-temporal && ./mvnw -q test -Dtest=ModelSerializationTest
```

Expected: PASS, 4 tests.

- [ ] **Step 5: Commit**

```bash
git add demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/model/ \
        demos/02-temporal/src/test/java/io/diagrid/enterprisediagnostics/model/ModelSerializationTest.java
git commit -m "feat(02-temporal): add model records with Jackson-compatible wire format"
```

---

## Task 4: `MockAnalysisGenerator` (random scan factory)

The .NET reference biases health to `[30, 65]` inclusive so the aggregate severity reliably hits `red_alert` for the on-stage demo. Anomaly count caps at 1/2/3 depending on health bucket.

**Files:**
- Create: `demos/02-temporal/src/test/java/io/diagrid/enterprisediagnostics/activity/MockAnalysisGeneratorTest.java`
- Create: `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/activity/MockAnalysisGenerator.java`

- [ ] **Step 1: Write the failing test**

```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanResult;
import org.junit.jupiter.api.RepeatedTest;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;

class MockAnalysisGeneratorTest {

    private static final List<String> CANDIDATES = List.of("a", "b", "c", "d", "e");

    @Test
    void resultEchoesSubsystemId() {
        ScanResult r = MockAnalysisGenerator.generate("warp_core", CANDIDATES);
        assertThat(r.subsystem()).isEqualTo("warp_core");
    }

    @RepeatedTest(100)
    void healthIsBetween30And65Inclusive() {
        ScanResult r = MockAnalysisGenerator.generate("warp_core", CANDIDATES);
        assertThat(r.health()).isBetween(30, 65);
    }

    @RepeatedTest(100)
    void anomaliesAreNonEmptyAndUniqueAndSubsetOfCandidates() {
        ScanResult r = MockAnalysisGenerator.generate("warp_core", CANDIDATES);
        assertThat(r.anomalies()).isNotEmpty();
        assertThat(r.anomalies()).doesNotHaveDuplicates();
        assertThat(CANDIDATES).containsAll(r.anomalies());
    }

    @RepeatedTest(50)
    void anomalyCountCapsAtThree() {
        ScanResult r = MockAnalysisGenerator.generate("warp_core", CANDIDATES);
        assertThat(r.anomalies().size()).isBetween(1, 3);
    }
}
```

- [ ] **Step 2: Run the test and verify it fails**

```bash
cd demos/02-temporal && ./mvnw -q test -Dtest=MockAnalysisGeneratorTest
```

Expected: FAIL — `MockAnalysisGenerator` class not found.

- [ ] **Step 3: Implement `MockAnalysisGenerator`**

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
        int health = RANDOM.nextInt(30, 66);   // 30..65 inclusive — biased low

        int maxAnomalies = (health >= 60) ? 1 : (health >= 45) ? 2 : 3;
        int anomalyCount = RANDOM.nextInt(1, maxAnomalies + 1);

        List<String> shuffled = new ArrayList<>(candidateAnomalies);
        Collections.shuffle(shuffled, RANDOM);
        List<String> picked = shuffled.subList(0, anomalyCount);

        return new ScanResult(subsystem, health, List.copyOf(picked));
    }
}
```

- [ ] **Step 4: Run the test and verify it passes**

```bash
cd demos/02-temporal && ./mvnw -q test -Dtest=MockAnalysisGeneratorTest
```

Expected: PASS — 1 + 100 + 100 + 50 = 251 tests.

- [ ] **Step 5: Commit**

```bash
git add demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/activity/MockAnalysisGenerator.java \
        demos/02-temporal/src/test/java/io/diagrid/enterprisediagnostics/activity/MockAnalysisGeneratorTest.java
git commit -m "feat(02-temporal): add MockAnalysisGenerator with biased-low health range"
```

---

## Task 5: Five scan activities

Each subsystem gets one `@ActivityInterface` and one `@ActivityImpl` (`@Component`) so each activity name is independently visible in Temporal Web — matching the .NET reference's per-subsystem class structure. Bodies are trivial (delegate to `MockAnalysisGenerator`), so no per-class unit test is needed; coverage comes from the workflow integration test in Task 8.

**Files (10 total — one interface + one impl per subsystem):**
- Create: `.../activity/ScanWarpCoreActivity.java` + `ScanWarpCoreActivityImpl.java`
- Create: `.../activity/ScanShieldsActivity.java` + `ScanShieldsActivityImpl.java`
- Create: `.../activity/ScanLifeSupportActivity.java` + `ScanLifeSupportActivityImpl.java`
- Create: `.../activity/ScanSensorsActivity.java` + `ScanSensorsActivityImpl.java`
- Create: `.../activity/ScanWeaponsActivity.java` + `ScanWeaponsActivityImpl.java`

The candidate-anomaly string lists come verbatim from `demos/04-dapr/EnterpriseDiagnostics.ApiService/Activities/Scan*Activity.cs`. Below uses the exact strings; do not paraphrase.

- [ ] **Step 1: Create `ScanWarpCoreActivity` interface**

```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import io.temporal.activity.ActivityInterface;

@ActivityInterface
public interface ScanWarpCoreActivity {
    ScanResult execute(ScanInput input);
}
```

- [ ] **Step 2: Create `ScanWarpCoreActivityImpl`**

```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import io.temporal.spring.boot.ActivityImpl;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
@ActivityImpl(taskQueues = "diagnostics-tq")
public class ScanWarpCoreActivityImpl implements ScanWarpCoreActivity {

    private static final Logger log = LoggerFactory.getLogger(ScanWarpCoreActivityImpl.class);

    private static final List<String> CANDIDATES = List.of(
        "Matter/antimatter containment field oscillating outside tolerance",
        "Dilithium crystal alignment drift detected at 0.04 microns",
        "Plasma injector #3 throughput below specification",
        "Warp field coil efficiency reduced by 6% on starboard nacelle",
        "EPS conduit overheating near main reactor manifold");

    @Override
    public ScanResult execute(ScanInput input) {
        log.info("ScanWarpCoreActivity: scanning warp core for {}", input.shipName());
        return MockAnalysisGenerator.generate(input.subsystem(), CANDIDATES);
    }
}
```

- [ ] **Step 3: Create `ScanShieldsActivity` (interface + impl)**

Interface — same shape as `ScanWarpCoreActivity`, replace the name.

Impl:
```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import io.temporal.spring.boot.ActivityImpl;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
@ActivityImpl(taskQueues = "diagnostics-tq")
public class ScanShieldsActivityImpl implements ScanShieldsActivity {

    private static final Logger log = LoggerFactory.getLogger(ScanShieldsActivityImpl.class);

    private static final List<String> CANDIDATES = List.of(
        "Deflector grid phase variance exceeding nominal range",
        "Shield generator 2 recharge rate slower than baseline",
        "Forward shield emitter coupling showing micro-arc events",
        "Harmonic resonance detected in dorsal shield projector",
        "EPS tap to aft shield array running 8% under spec");

    @Override
    public ScanResult execute(ScanInput input) {
        log.info("ScanShieldsActivity: scanning shields for {}", input.shipName());
        return MockAnalysisGenerator.generate(input.subsystem(), CANDIDATES);
    }
}
```

- [ ] **Step 4: Create `ScanLifeSupportActivity` (interface + impl)**

```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import io.temporal.activity.ActivityInterface;

@ActivityInterface
public interface ScanLifeSupportActivity {
    ScanResult execute(ScanInput input);
}
```

```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import io.temporal.spring.boot.ActivityImpl;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
@ActivityImpl(taskQueues = "diagnostics-tq")
public class ScanLifeSupportActivityImpl implements ScanLifeSupportActivity {

    private static final Logger log = LoggerFactory.getLogger(ScanLifeSupportActivityImpl.class);

    private static final List<String> CANDIDATES = List.of(
        "Atmospheric CO2 scrubber efficiency reduced by 4.7% on deck 9",
        "Environmental control humidity regulators showing intermittent faults in crew quarters",
        "Oxygen recycler intake filter contamination detected in main air processor",
        "Thermal regulation imbalance reported across deck 12 corridor sections",
        "Gravity plating fluctuation outside nominal range in cargo bay 4");

    @Override
    public ScanResult execute(ScanInput input) {
        log.info("ScanLifeSupportActivity: scanning life support for {}", input.shipName());
        return MockAnalysisGenerator.generate(input.subsystem(), CANDIDATES);
    }
}
```

- [ ] **Step 5: Create `ScanSensorsActivity` (interface + impl)**

Interface (same shape, name change).

Impl:
```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import io.temporal.spring.boot.ActivityImpl;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
@ActivityImpl(taskQueues = "diagnostics-tq")
public class ScanSensorsActivityImpl implements ScanSensorsActivity {

    private static final Logger log = LoggerFactory.getLogger(ScanSensorsActivityImpl.class);

    private static final List<String> CANDIDATES = List.of(
        "Lateral sensor array gain calibration drift detected",
        "Subspace bandwidth allocation below recommended threshold",
        "Long-range sensor pallet 3 reporting elevated noise floor",
        "Navigational deflector return signal showing phase jitter",
        "Tachyon detection grid misaligned by 0.12 degrees");

    @Override
    public ScanResult execute(ScanInput input) {
        log.info("ScanSensorsActivity: scanning sensors for {}", input.shipName());
        return MockAnalysisGenerator.generate(input.subsystem(), CANDIDATES);
    }
}
```

- [ ] **Step 6: Create `ScanWeaponsActivity` (interface + impl)**

Interface (same shape, name change).

Impl:
```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import io.temporal.spring.boot.ActivityImpl;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
@ActivityImpl(taskQueues = "diagnostics-tq")
public class ScanWeaponsActivityImpl implements ScanWeaponsActivity {

    private static final Logger log = LoggerFactory.getLogger(ScanWeaponsActivityImpl.class);

    private static final List<String> CANDIDATES = List.of(
        "Phaser array bank 3 power coupling shows intermittent drop",
        "Photon torpedo launcher loading mechanism response time elevated",
        "Targeting sensor calibration drift detected on aft array",
        "Tactical computer subroutine queue depth exceeding threshold",
        "Quantum torpedo magazine humidity outside recommended range");

    @Override
    public ScanResult execute(ScanInput input) {
        log.info("ScanWeaponsActivity: scanning weapons for {}", input.shipName());
        return MockAnalysisGenerator.generate(input.subsystem(), CANDIDATES);
    }
}
```

- [ ] **Step 7: Verify the project still compiles**

```bash
cd demos/02-temporal && ./mvnw -q clean compile
```

Expected: BUILD SUCCESS.

- [ ] **Step 8: Commit**

```bash
git add demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/activity/Scan*.java
git commit -m "feat(02-temporal): add 5 subsystem scan activities (one class per subsystem)"
```

---

## Task 6: `PrioritizeIssuesActivity` (deterministic severity calculation)

This is the only scan-side activity with branching logic that's worth unit-testing directly.

**Files:**
- Create: `demos/02-temporal/src/test/java/io/diagrid/enterprisediagnostics/activity/PrioritizeIssuesActivityImplTest.java`
- Create: `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/activity/PrioritizeIssuesActivity.java`
- Create: `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/activity/PrioritizeIssuesActivityImpl.java`

- [ ] **Step 1: Write the failing test**

```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.PrioritizeInput;
import io.diagrid.enterprisediagnostics.model.Priority;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import io.diagrid.enterprisediagnostics.model.Severity;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;

class PrioritizeIssuesActivityImplTest {

    private final PrioritizeIssuesActivity activity = new PrioritizeIssuesActivityImpl();

    @Test
    void minHealthBelow50YieldsRedAlert() {
        Priority p = activity.execute(new PrioritizeInput("ship", List.of(
            new ScanResult("warp_core",    40, List.of("x")),
            new ScanResult("shields",      80, List.of("y")),
            new ScanResult("life_support", 90, List.of("z")))));
        assertThat(p.severity()).isEqualTo(Severity.RED_ALERT);
        assertThat(p.summary()).contains("warp_core").contains("40");
    }

    @Test
    void minHealthBetween50And74YieldsYellowAlert() {
        Priority p = activity.execute(new PrioritizeInput("ship", List.of(
            new ScanResult("warp_core", 60, List.of("x")),
            new ScanResult("shields",   90, List.of("y")))));
        assertThat(p.severity()).isEqualTo(Severity.YELLOW_ALERT);
    }

    @Test
    void minHealthAtOrAbove75YieldsGreen() {
        Priority p = activity.execute(new PrioritizeInput("ship", List.of(
            new ScanResult("warp_core", 80, List.of("x")),
            new ScanResult("shields",   95, List.of("y")))));
        assertThat(p.severity()).isEqualTo(Severity.GREEN);
    }

    @Test
    void summaryReportsBelowThresholdCount() {
        Priority p = activity.execute(new PrioritizeInput("ship", List.of(
            new ScanResult("warp_core",    40, List.of("x")),
            new ScanResult("shields",      60, List.of("y")),
            new ScanResult("life_support", 90, List.of("z")))));
        // 2 of 3 below 75
        assertThat(p.summary()).contains("2");
    }
}
```

- [ ] **Step 2: Run the test and verify it fails**

```bash
cd demos/02-temporal && ./mvnw -q test -Dtest=PrioritizeIssuesActivityImplTest
```

Expected: FAIL — types not found.

- [ ] **Step 3: Create the interface**

```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.PrioritizeInput;
import io.diagrid.enterprisediagnostics.model.Priority;
import io.temporal.activity.ActivityInterface;

@ActivityInterface
public interface PrioritizeIssuesActivity {
    Priority execute(PrioritizeInput input);
}
```

- [ ] **Step 4: Create the impl**

```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.PrioritizeInput;
import io.diagrid.enterprisediagnostics.model.Priority;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import io.diagrid.enterprisediagnostics.model.Severity;
import io.temporal.spring.boot.ActivityImpl;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

import java.util.Comparator;

@Component
@ActivityImpl(taskQueues = "diagnostics-tq")
public class PrioritizeIssuesActivityImpl implements PrioritizeIssuesActivity {

    private static final Logger log = LoggerFactory.getLogger(PrioritizeIssuesActivityImpl.class);

    @Override
    public Priority execute(PrioritizeInput input) {
        log.info("PrioritizeIssuesActivity: prioritizing for {}", input.shipName());

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

        String summary = "Worst subsystem: %s @ %d%% health. %d of %d subsystems below operational threshold."
            .formatted(worst.subsystem(), worst.health(), belowThreshold, input.scans().size());

        return new Priority(severity, summary);
    }
}
```

(The summary string matches the .NET reference's format from `PrioritizeIssuesActivity.cs` so the demo's status output is identical.)

- [ ] **Step 5: Run the test and verify it passes**

```bash
cd demos/02-temporal && ./mvnw -q test -Dtest=PrioritizeIssuesActivityImplTest
```

Expected: PASS, 4 tests.

- [ ] **Step 6: Commit**

```bash
git add demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/activity/PrioritizeIssues*.java \
        demos/02-temporal/src/test/java/io/diagrid/enterprisediagnostics/activity/PrioritizeIssuesActivityImplTest.java
git commit -m "feat(02-temporal): add PrioritizeIssuesActivity with severity thresholds"
```

---

## Task 7: `NotifyBridgeActivity`

**Files:**
- Create: `demos/02-temporal/src/test/java/io/diagrid/enterprisediagnostics/activity/NotifyBridgeActivityImplTest.java`
- Create: `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/activity/NotifyBridgeActivity.java`
- Create: `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/activity/NotifyBridgeActivityImpl.java`

- [ ] **Step 1: Write the failing test**

```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.BridgeNotificationInput;
import io.diagrid.enterprisediagnostics.model.BridgeNotificationResult;
import io.diagrid.enterprisediagnostics.model.Priority;
import io.diagrid.enterprisediagnostics.model.Severity;
import org.junit.jupiter.api.Test;

import static org.assertj.core.api.Assertions.assertThat;

class NotifyBridgeActivityImplTest {

    private final NotifyBridgeActivity activity = new NotifyBridgeActivityImpl();

    @Test
    void messageFollowsCanonicalFormat() {
        var priority = new Priority(Severity.RED_ALERT, "warp core critical at 40% health");
        var input = new BridgeNotificationInput(
            "Enterprise NCC-1701-D", "Geordi La Forge", priority);

        BridgeNotificationResult result = activity.execute(input);

        assertThat(result.message()).isEqualTo(
            "🚨 BRIDGE ALERT — USS Enterprise NCC-1701-D — RED_ALERT: " +
            "warp core critical at 40% health (engineer: Geordi La Forge)");
    }

    @Test
    void severityIsUppercasedInMessage() {
        var priority = new Priority(Severity.YELLOW_ALERT, "minor issue");
        var input = new BridgeNotificationInput("Voyager", "B'Elanna", priority);

        assertThat(activity.execute(input).message()).contains("YELLOW_ALERT");
    }
}
```

- [ ] **Step 2: Run the test and verify it fails**

```bash
cd demos/02-temporal && ./mvnw -q test -Dtest=NotifyBridgeActivityImplTest
```

Expected: FAIL.

- [ ] **Step 3: Create the interface**

```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.BridgeNotificationInput;
import io.diagrid.enterprisediagnostics.model.BridgeNotificationResult;
import io.temporal.activity.ActivityInterface;

@ActivityInterface
public interface NotifyBridgeActivity {
    BridgeNotificationResult execute(BridgeNotificationInput input);
}
```

- [ ] **Step 4: Create the impl**

```java
package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.BridgeNotificationInput;
import io.diagrid.enterprisediagnostics.model.BridgeNotificationResult;
import io.temporal.spring.boot.ActivityImpl;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

@Component
@ActivityImpl(taskQueues = "diagnostics-tq")
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

- [ ] **Step 5: Run the test and verify it passes**

```bash
cd demos/02-temporal && ./mvnw -q test -Dtest=NotifyBridgeActivityImplTest
```

Expected: PASS, 2 tests.

- [ ] **Step 6: Commit**

```bash
git add demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/activity/NotifyBridge*.java \
        demos/02-temporal/src/test/java/io/diagrid/enterprisediagnostics/activity/NotifyBridgeActivityImplTest.java
git commit -m "feat(02-temporal): add NotifyBridgeActivity emitting canonical alert message"
```

---

## Task 8: `DiagnosticsWorkflow` + `TestWorkflowEnvironment` integration test

This is the heart of the demo. Test the *workflow shape* (fan-out, durable timer, conditional notify) using Temporal's in-memory test environment.

**Files:**
- Create: `demos/02-temporal/src/test/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflowImplTest.java`
- Create: `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflow.java`
- Create: `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflowImpl.java`

- [ ] **Step 1: Write the failing test**

```java
package io.diagrid.enterprisediagnostics.workflow;

import io.diagrid.enterprisediagnostics.activity.*;
import io.diagrid.enterprisediagnostics.model.*;
import io.temporal.client.WorkflowClient;
import io.temporal.client.WorkflowOptions;
import io.temporal.testing.TestWorkflowEnvironment;
import io.temporal.worker.Worker;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.*;

class DiagnosticsWorkflowImplTest {

    private TestWorkflowEnvironment env;
    private Worker worker;
    private WorkflowClient client;

    private ScanWarpCoreActivity warp;
    private ScanShieldsActivity shields;
    private ScanLifeSupportActivity life;
    private ScanSensorsActivity sensors;
    private ScanWeaponsActivity weapons;
    private PrioritizeIssuesActivity prioritize;
    private NotifyBridgeActivity notify;

    @BeforeEach
    void setUp() {
        env = TestWorkflowEnvironment.newInstance();
        worker = env.newWorker("diagnostics-tq");
        worker.registerWorkflowImplementationTypes(DiagnosticsWorkflowImpl.class);

        warp       = mock(ScanWarpCoreActivity.class);
        shields    = mock(ScanShieldsActivity.class);
        life       = mock(ScanLifeSupportActivity.class);
        sensors    = mock(ScanSensorsActivity.class);
        weapons    = mock(ScanWeaponsActivity.class);
        prioritize = mock(PrioritizeIssuesActivity.class);
        notify     = mock(NotifyBridgeActivity.class);

        worker.registerActivitiesImplementations(
            warp, shields, life, sensors, weapons, prioritize, notify);

        env.start();
        client = env.getWorkflowClient();
    }

    @AfterEach
    void tearDown() {
        env.close();
    }

    @Test
    void redAlertPathInvokesAllFiveScansThenPrioritizeThenNotify() {
        when(warp.execute(any())).thenReturn(new ScanResult("warp_core",    40, List.of("a")));
        when(shields.execute(any())).thenReturn(new ScanResult("shields",   80, List.of("b")));
        when(life.execute(any())).thenReturn(new ScanResult("life_support", 90, List.of("c")));
        when(sensors.execute(any())).thenReturn(new ScanResult("sensors",   85, List.of("d")));
        when(weapons.execute(any())).thenReturn(new ScanResult("weapons",   95, List.of("e")));

        when(prioritize.execute(any()))
            .thenReturn(new Priority(Severity.RED_ALERT, "warp_core critical"));
        when(notify.execute(any()))
            .thenReturn(new BridgeNotificationResult("BRIDGE ALERT message"));

        DiagnosticsWorkflow stub = client.newWorkflowStub(
            DiagnosticsWorkflow.class,
            WorkflowOptions.newBuilder()
                .setWorkflowId("test-red")
                .setTaskQueue("diagnostics-tq")
                .build());

        DiagnosticsOutput out = stub.runDiagnostics(new DiagnosticsInput(
            "test-red", "Enterprise", "2370-04-08", "Geordi"));

        assertThat(out.priority().severity()).isEqualTo(Severity.RED_ALERT);
        assertThat(out.bridgeNotification()).isNotNull();
        assertThat(out.bridgeNotification().message()).isEqualTo("BRIDGE ALERT message");

        verify(warp, times(1)).execute(any());
        verify(shields, times(1)).execute(any());
        verify(life, times(1)).execute(any());
        verify(sensors, times(1)).execute(any());
        verify(weapons, times(1)).execute(any());
        verify(prioritize, times(1)).execute(any());
        verify(notify, times(1)).execute(any());
    }

    @Test
    void greenPathSkipsNotify() {
        when(warp.execute(any())).thenReturn(new ScanResult("warp_core",    90, List.of("a")));
        when(shields.execute(any())).thenReturn(new ScanResult("shields",   95, List.of("b")));
        when(life.execute(any())).thenReturn(new ScanResult("life_support", 92, List.of("c")));
        when(sensors.execute(any())).thenReturn(new ScanResult("sensors",   88, List.of("d")));
        when(weapons.execute(any())).thenReturn(new ScanResult("weapons",   85, List.of("e")));

        when(prioritize.execute(any()))
            .thenReturn(new Priority(Severity.GREEN, "all nominal"));

        DiagnosticsWorkflow stub = client.newWorkflowStub(
            DiagnosticsWorkflow.class,
            WorkflowOptions.newBuilder()
                .setWorkflowId("test-green")
                .setTaskQueue("diagnostics-tq")
                .build());

        DiagnosticsOutput out = stub.runDiagnostics(new DiagnosticsInput(
            "test-green", "Enterprise", "2370-04-08", "Geordi"));

        assertThat(out.priority().severity()).isEqualTo(Severity.GREEN);
        assertThat(out.bridgeNotification()).isNull();
        verify(notify, never()).execute(any());
    }
}
```

Note: `TestWorkflowEnvironment` skips the 60s timer automatically (test-time clock); the test is fast.

- [ ] **Step 2: Run the test and verify it fails**

```bash
cd demos/02-temporal && ./mvnw -q test -Dtest=DiagnosticsWorkflowImplTest
```

Expected: FAIL — `DiagnosticsWorkflow` and `DiagnosticsWorkflowImpl` don't exist.

- [ ] **Step 3: Create the workflow interface**

```java
package io.diagrid.enterprisediagnostics.workflow;

import io.diagrid.enterprisediagnostics.model.DiagnosticsInput;
import io.diagrid.enterprisediagnostics.model.DiagnosticsOutput;
import io.temporal.workflow.WorkflowInterface;
import io.temporal.workflow.WorkflowMethod;

@WorkflowInterface
public interface DiagnosticsWorkflow {
    @WorkflowMethod
    DiagnosticsOutput runDiagnostics(DiagnosticsInput input);
}
```

- [ ] **Step 4: Create the workflow impl**

```java
package io.diagrid.enterprisediagnostics.workflow;

import io.diagrid.enterprisediagnostics.activity.*;
import io.diagrid.enterprisediagnostics.model.*;
import io.temporal.activity.ActivityOptions;
import io.temporal.common.RetryOptions;
import io.temporal.spring.boot.WorkflowImpl;
import io.temporal.workflow.Async;
import io.temporal.workflow.Promise;
import io.temporal.workflow.Workflow;
import org.slf4j.Logger;

import java.time.Duration;
import java.util.List;

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

    private final ScanWarpCoreActivity     warpCore    = Workflow.newActivityStub(ScanWarpCoreActivity.class,     ACTIVITY_OPTIONS);
    private final ScanShieldsActivity      shields     = Workflow.newActivityStub(ScanShieldsActivity.class,      ACTIVITY_OPTIONS);
    private final ScanLifeSupportActivity  lifeSupport = Workflow.newActivityStub(ScanLifeSupportActivity.class,  ACTIVITY_OPTIONS);
    private final ScanSensorsActivity      sensors     = Workflow.newActivityStub(ScanSensorsActivity.class,      ACTIVITY_OPTIONS);
    private final ScanWeaponsActivity      weapons     = Workflow.newActivityStub(ScanWeaponsActivity.class,      ACTIVITY_OPTIONS);
    private final PrioritizeIssuesActivity prioritize  = Workflow.newActivityStub(PrioritizeIssuesActivity.class, ACTIVITY_OPTIONS);
    private final NotifyBridgeActivity     notify      = Workflow.newActivityStub(NotifyBridgeActivity.class,     ACTIVITY_OPTIONS);

    @Override
    public DiagnosticsOutput runDiagnostics(DiagnosticsInput input) {
        Logger logger = Workflow.getLogger(DiagnosticsWorkflowImpl.class);
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
        logger.info("All scans complete for {}", input.shipName());

        // ⚡ KILL POINT — durable timer ⚡
        logger.info("Engineering review pending — sleeping 60s");
        Workflow.sleep(Duration.ofSeconds(60));

        Priority priority = prioritize.execute(new PrioritizeInput(input.shipName(), scans));
        logger.info("Prioritized with severity: {}", priority.severity());

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

- [ ] **Step 5: Run the test and verify it passes**

```bash
cd demos/02-temporal && ./mvnw -q test -Dtest=DiagnosticsWorkflowImplTest
```

Expected: PASS, 2 tests. The `Workflow.sleep(60s)` is auto-skipped by `TestWorkflowEnvironment` so each test runs in under a second.

- [ ] **Step 6: Run the entire test suite**

```bash
cd demos/02-temporal && ./mvnw -q test
```

Expected: all tests from Tasks 2–8 pass.

- [ ] **Step 7: Commit**

```bash
git add demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/workflow/ \
        demos/02-temporal/src/test/java/io/diagrid/enterprisediagnostics/workflow/
git commit -m "feat(02-temporal): add DiagnosticsWorkflow with fan-out + durable timer + conditional notify"
```

---

## Task 9: REST controller — `/start`, `/status`, `/terminate`, `/cancel`

**Files:**
- Create: `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/api/DiagnosticsController.java`

No test in this task — coverage comes from the end-to-end manual smoke run in Task 10 and Task 11. Adding a `@SpringBootTest` here would need a real Temporal connection or extensive mocking, and the cost outweighs the value for a stage demo.

- [ ] **Step 1: Create the controller**

```java
package io.diagrid.enterprisediagnostics.api;

import io.diagrid.enterprisediagnostics.model.DiagnosticsInput;
import io.diagrid.enterprisediagnostics.model.DiagnosticsOutput;
import io.diagrid.enterprisediagnostics.workflow.DiagnosticsWorkflow;
import io.temporal.api.enums.v1.WorkflowExecutionStatus;
import io.temporal.client.WorkflowClient;
import io.temporal.client.WorkflowOptions;
import io.temporal.client.WorkflowStub;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.LinkedHashMap;
import java.util.Map;

@RestController
public class DiagnosticsController {

    private static final String TASK_QUEUE = "diagnostics-tq";

    private final WorkflowClient client;

    public DiagnosticsController(WorkflowClient client) {
        this.client = client;
    }

    @PostMapping("/start")
    public ResponseEntity<Map<String, String>> start(@RequestBody DiagnosticsInput input) {
        DiagnosticsWorkflow stub = client.newWorkflowStub(
            DiagnosticsWorkflow.class,
            WorkflowOptions.newBuilder()
                .setWorkflowId(input.id())
                .setTaskQueue(TASK_QUEUE)
                .build());

        WorkflowClient.start(stub::runDiagnostics, input);

        return ResponseEntity.ok(Map.of("workflowId", input.id()));
    }

    @GetMapping("/status/{id}")
    public ResponseEntity<Map<String, Object>> status(@PathVariable String id) {
        WorkflowStub stub = client.newUntypedWorkflowStub(id);
        WorkflowExecutionStatus status =
            stub.describe().getRawDescription().getWorkflowExecutionInfo().getStatus();

        Map<String, Object> body = new LinkedHashMap<>();
        body.put("workflowId", id);
        body.put("status", status.name());

        if (status == WorkflowExecutionStatus.WORKFLOW_EXECUTION_STATUS_COMPLETED) {
            DiagnosticsOutput output = client.newUntypedWorkflowStub(id)
                .getResult(DiagnosticsOutput.class);
            body.put("output", output);
        }
        return ResponseEntity.ok(body);
    }

    @PostMapping("/terminate/{id}")
    public ResponseEntity<Void> terminate(@PathVariable String id) {
        client.newUntypedWorkflowStub(id).terminate("manual termination");
        return ResponseEntity.accepted().build();
    }

    @PostMapping("/cancel/{id}")
    public ResponseEntity<Void> cancel(@PathVariable String id) {
        client.newUntypedWorkflowStub(id).cancel();
        return ResponseEntity.accepted().build();
    }
}
```

(Temporal Java has no first-class `suspend`/`resume`. The README in Task 11 calls this out honestly — `/cancel` is the available analog of Dapr's pause.)

- [ ] **Step 2: Verify compilation**

```bash
cd demos/02-temporal && ./mvnw -q clean compile
```

Expected: BUILD SUCCESS.

- [ ] **Step 3: Commit**

```bash
git add demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/api/DiagnosticsController.java
git commit -m "feat(02-temporal): add REST controller for start/status/terminate/cancel"
```

---

## Task 10: `application.yml` + worker auto-discovery smoke test

This is where the Spring starter is wired up. After this task the app connects to Temporal at boot.

**Files:**
- Modify: `demos/02-temporal/src/main/resources/application.yml`

- [ ] **Step 1: Replace `application.yml`**

```yaml
spring:
  application:
    name: enterprise-diagnostics
  temporal:
    namespace: default
    connection:
      target: 127.0.0.1:7233
    workers:
      - task-queue: diagnostics-tq
        name: diagnostics-worker
    workers-auto-discovery:
      packages:
        - io.diagrid.enterprisediagnostics.workflow
        - io.diagrid.enterprisediagnostics.activity

server:
  port: 8080

logging:
  level:
    io.diagrid.enterprisediagnostics: INFO
    io.temporal: INFO
```

(The starter's `workers-auto-discovery.packages` block tells it where to scan for `@WorkflowImpl` and `@ActivityImpl` beans. If a future starter version uses a different key, update this in lock-step.)

- [ ] **Step 2: Smoke test — Temporal dev server + Spring Boot app**

In one terminal:
```bash
temporal server start-dev
```

Expected: `Temporal Server` banner, `Server: gRPC :7233`, `UI: http://localhost:8233`.

In a second terminal:
```bash
cd demos/02-temporal && ./mvnw spring-boot:run
```

Expected log lines:
- `Started EnterpriseDiagnosticsApplication in N.NNN seconds`
- A Temporal log line like `Started Worker with TaskQueue=diagnostics-tq` (text varies by starter version).
- No exceptions related to `temporal-spring-boot-starter`.

- [ ] **Step 3: Start a workflow and watch it run end-to-end**

In a third terminal:
```bash
curl -s -X POST http://localhost:8080/start \
  -H 'Content-Type: application/json' \
  -d '{
    "id": "enterprise-ncc-1701",
    "shipName": "USS Enterprise NCC-1701-D",
    "diagnosticsDate": "2370-04-08",
    "engineerName": "Geordi La Forge"
  }'
```

Expected: `{"workflowId":"enterprise-ncc-1701"}`.

Open `http://localhost:8233` in a browser and confirm `enterprise-ncc-1701` is `RUNNING`, with 5 `ActivityTaskCompleted` events near the top and a `TimerStarted` event waiting.

Wait ~70 seconds and check status:
```bash
curl -s http://localhost:8080/status/enterprise-ncc-1701 | jq .
```

Expected:
- `"status":"WORKFLOW_EXECUTION_STATUS_COMPLETED"`
- `"output"` includes a non-null `bridgeNotification.message` starting with `🚨 BRIDGE ALERT — USS USS Enterprise NCC-1701-D`.

If the bridge message is `null` after multiple runs, recheck `PrioritizeIssuesActivityImpl` thresholds vs `MockAnalysisGenerator.generate` health range.

Stop the dev server (`Ctrl+C` in terminal 1) and the Spring Boot app (`Ctrl+C` in terminal 2) before continuing.

- [ ] **Step 4: Commit**

```bash
git add demos/02-temporal/src/main/resources/application.yml
git commit -m "feat(02-temporal): wire Temporal Spring Boot starter to dev server"
```

---

## Task 11: `.http` file + README + rehearsal verification

**Files:**
- Create: `demos/02-temporal/enterprise-diagnostics.http`
- Create: `demos/02-temporal/README.md`

- [ ] **Step 1: Create `enterprise-diagnostics.http`**

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

### Cancel (Temporal's analog of Dapr Suspend — there's no Resume)
POST http://localhost:8080/cancel/enterprise-ncc-1701

### Terminate
POST http://localhost:8080/terminate/enterprise-ncc-1701
```

- [ ] **Step 2: Create `README.md`**

````markdown
# Temporal (Java) — Enterprise Diagnostics Demo

Stage demo #2 in the **Durable Execution Battle** talk. Workflow shape, severity thresholds, and bridge-message format match the canonical scenario in [`../shared/scenario.md`](../shared/scenario.md). The .NET reference at [`../04-dapr/`](../04-dapr/) is the scenario source of truth.

## Stack

- Java 21 LTS
- Spring Boot 3.4.x
- `io.temporal:temporal-sdk` 1.30.x + `temporal-spring-boot-starter-alpha` (production-quality despite the `-alpha` artifact name; pinned in `pom.xml`)
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

## Tests

```bash
cd demos/02-temporal && ./mvnw test
```

Includes:
- `SeverityJsonTest`, `ModelSerializationTest` — wire-format guards
- `MockAnalysisGeneratorTest` — property tests over the biased-low random generator
- `PrioritizeIssuesActivityImplTest` — severity threshold rules
- `NotifyBridgeActivityImplTest` — canonical bridge message format
- `DiagnosticsWorkflowImplTest` — uses `TestWorkflowEnvironment` to exercise fan-out + conditional notify (60 s timer is skipped by the test clock)
````

- [ ] **Step 3: Run the full verification checklist (from spec §15)**

```bash
# Pre-flight
cd demos/02-temporal && ./mvnw -q clean verify
```

Expected: BUILD SUCCESS, all tests pass.

```bash
# Manual stage rehearsal — run twice end-to-end
temporal server start-dev    # terminal 1
cd demos/02-temporal && ./mvnw spring-boot:run    # terminal 2

# Terminal 3:
for i in 1 2 3 4 5; do
  ID="enterprise-rehearsal-$i"
  curl -s -X POST http://localhost:8080/start \
    -H 'Content-Type: application/json' \
    -d "{\"id\":\"$ID\",\"shipName\":\"USS Enterprise NCC-1701-D\",\"diagnosticsDate\":\"2370-04-08\",\"engineerName\":\"Geordi La Forge\"}" > /dev/null
  sleep 65
  curl -s "http://localhost:8080/status/$ID" | jq '.output.bridgeNotification.message'
done
```

Expected: 5 non-null `bridgeNotification.message` strings (rigging holds). If any is `null`, re-check thresholds.

Kill-and-resume drill:
```bash
# Start a workflow
curl -X POST http://localhost:8080/start -H 'Content-Type: application/json' \
  -d '{"id":"kill-resume-1","shipName":"USS Enterprise NCC-1701-D","diagnosticsDate":"2370-04-08","engineerName":"Geordi La Forge"}'
# Wait ~20s, then Ctrl+C the Spring Boot app
# Restart: ./mvnw spring-boot:run
# Wait until total ~70s elapsed since /start, then:
curl -s http://localhost:8080/status/kill-resume-1 | jq '.status, .output.bridgeNotification.message'
```

Expected: `"WORKFLOW_EXECUTION_STATUS_COMPLETED"` and a non-null bridge message — the durable timer survived the worker restart.

- [ ] **Step 4: Commit**

```bash
git add demos/02-temporal/enterprise-diagnostics.http demos/02-temporal/README.md
git commit -m "docs(02-temporal): add README and HTTP request file for on-stage demo"
```

---

## Task 12: Apply `plan.md` updates (numbering + content)

Spec §11 describes how the parent `plan.md` must shift to reflect Cadence → Temporal (Java) as positions `01-` and `02-`. The Cadence folder (`02-cadence/`) and Python placeholder (`01-temporal/`) **do not exist yet** in `demos/`, so this task is purely about updating the planning document, not moving files.

**Files:**
- Modify: `/Users/marcduiker/dev/diagrid-labs/durable-execution-battle/plan.md`

- [ ] **Step 1: Read the current `plan.md`**

```bash
cd /Users/marcduiker/dev/diagrid-labs/durable-execution-battle && wc -l plan.md
```

Open in an editor; locate each section listed in spec §11 (Locked decisions table, §1 timeline, §2 repo layout, §2 `.demo` list, §5 subsection ordering, §5.2 Temporal subsection, §5.2 idiomatic tip, §7 pre-talk checklist, §8 authoring order).

- [ ] **Step 2: Apply each `plan.md` edit listed in spec §11**

Each row of the spec's §11 table is a concrete edit:

1. **Locked-decisions table:** "Temporal **Python**" → "Temporal **Java**".
2. **§1 timeline:** Reorder demo block to `Cadence (Go) → Temporal (Java) → Conductor (Python) → Dapr (.NET) → Restate (TS)`. Times: 18:00 / 22:00 / 27:00 / 32:00 / 38:00.
3. **§2 repo layout:** Delete the `01-temporal/` Python block. Rename `02-cadence/` → `01-cadence/`. Insert a `02-temporal/` (Java/Maven) block matching the tree at the top of this plan.
4. **§2 `.demo` files:** Remove `01-temporal.demo`. Rename `02-cadence.demo` → `01-cadence.demo`. Add `02-temporal.demo`. Keep `03-/04-/05-` as is.
5. **§5 subsection order:** §5.1 Cadence, §5.2 Temporal (Java), §5.3 Conductor, §5.4 Dapr, §5.5 Restate. Cadence subsection's file paths shift to `demos/01-cadence/...`.
6. **§5.2 Temporal rewrite (Java):** Reference `demos/02-temporal/src/main/java/io/diagrid/enterprisediagnostics/workflow/DiagnosticsWorkflowImpl.java`, the `@WorkflowInterface` / `Async.function` / `Promise.allOf` shape, `Workflow.sleep(Duration.ofSeconds(60))`, runtime = `temporal server start-dev` + `./mvnw spring-boot:run`, UI at `localhost:8233`.
7. **§5.2 idiomatic tip:** "Workflow code is a sandboxed deterministic class — no `Random`, no `LocalDateTime.now()`, no `Thread.sleep`. Activities are normal Spring beans where the side-effects live."
8. **§7 pre-talk checklist:** Add `brew install temporal` (no Docker pull) and `./mvnw dependency:go-offline` in `demos/02-temporal/` to warm the Maven cache.
9. **§8 authoring order:** Update the Temporal-demo line: Python → Java; bump time estimate if needed. Authoring order ("Dapr → Temporal → Restate → Conductor → Cadence") stays the same.

- [ ] **Step 3: Sanity-check `plan.md`**

Re-read the modified file end-to-end. Confirm:
- No remaining "Temporal **Python**" strings.
- No `01-temporal/` paths referenced outside of historical context.
- Section §5 subsection numbering is sequential and consistent.
- Total timeline math still sums to the talk length.

- [ ] **Step 4: Commit**

```bash
cd /Users/marcduiker/dev/diagrid-labs/durable-execution-battle
git add plan.md
git commit -m "docs: realign plan.md for Cadence (01) + Temporal-Java (02) ordering"
```

(If the working tree already has `M plan.md` staged from earlier work — visible in the initial `git status` — fold those changes in with `git add plan.md` and one commit.)

---

## Self-review notes

- **Spec coverage:** Tasks 1–11 cover every spec section: §2 decisions (Task 1 pom + Task 10 yml), §3 layout (Tasks 1–9), §4 models (Tasks 2–3), §5 workflow (Task 8), §6 activities (Tasks 4–7), §7 HTTP API (Task 9), §8 runtime (Task 10), §9 pom highlights (Task 1), §10 application.yml (Task 10), §11 plan.md updates (Task 12), §12 mapping table (README in Task 11), §13 out-of-scope explicitly avoided, §14 risks mitigated inline (Severity test, pinned versions, `Workflow.sleep` chosen over `newTimer`), §15 verification checklist (Task 11 Step 3).
- **No placeholders:** every step has actual code, exact commands, expected output.
- **Type consistency:** `Severity` is `enum` everywhere; `bridgeNotification` is `BridgeNotificationResult` (not `String`) throughout — tests and impls aligned. Activity method is always `execute(...)`. Workflow method is always `runDiagnostics(...)`. Task queue is always `"diagnostics-tq"`.

