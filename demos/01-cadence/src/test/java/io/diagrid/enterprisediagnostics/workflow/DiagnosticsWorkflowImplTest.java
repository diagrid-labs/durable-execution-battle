package io.diagrid.enterprisediagnostics.workflow;

import com.uber.cadence.client.WorkflowClient;
import com.uber.cadence.client.WorkflowClientOptions;
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

    private static final String DOMAIN    = "test-domain";
    private static final String TASK_LIST = "diagnostics-tl-test";

    private TestWorkflowEnvironment env;
    private WorkflowClient          client;

    @BeforeEach
    void setUp() {
        // TestEnvironmentOptions.Builder has no setDomain(); domain is set via
        // WorkflowClientOptions which is passed into TestEnvironmentOptions.
        // WorkflowClientOptions.Builder() constructor is private — use newBuilder() factory.
        WorkflowClientOptions clientOptions = WorkflowClientOptions.newBuilder()
            .setDomain(DOMAIN)
            .build();

        TestEnvironmentOptions testOptions = new TestEnvironmentOptions.Builder()
            .setWorkflowClientOptions(clientOptions)
            .build();

        env = TestWorkflowEnvironment.newInstance(testOptions);

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

    // MockAnalysisGenerator always produces health in [30, 65].
    // The worst of 5 scans is always < 75, so severity is always YELLOW_ALERT or RED_ALERT —
    // bridge notification always fires. The repeated test verifies the happy path is stable.
    @RepeatedTest(10)
    void runsHappyPathWithBridgeNotificationAlwaysFiring() {
        DiagnosticsWorkflow workflow = client.newWorkflowStub(
            DiagnosticsWorkflow.class,
            new WorkflowOptions.Builder()
                .setTaskList(TASK_LIST)
                .setWorkflowId("test-" + System.nanoTime())
                .setExecutionStartToCloseTimeout(Duration.ofMinutes(5))
                .build());

        DiagnosticsOutput out = workflow.runDiagnostics(new DiagnosticsInput(
            "test-1", "USS Enterprise NCC-1701-D", "2370-04-08", "Geordi La Forge"));

        // MockAnalysisGenerator health range [30,65] guarantees worst < 75,
        // so severity is always YELLOW_ALERT or RED_ALERT — never GREEN.
        assertNotEquals(Severity.GREEN, out.priority().severity(),
            "health [30,65] should never yield GREEN");

        // Bridge notification fires for YELLOW_ALERT and RED_ALERT.
        assertNotNull(out.bridgeNotification(), "bridge notification should always fire");
        assertTrue(out.bridgeNotification().message().contains("🚨 BRIDGE ALERT"),
            "got: " + out.bridgeNotification().message());

        // Message should contain the actual severity name (YELLOW_ALERT or RED_ALERT).
        String severityName = out.priority().severity().name();
        assertTrue(out.bridgeNotification().message().contains(severityName),
            "message should contain severity " + severityName + "; got: " + out.bridgeNotification().message());

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
        // TestWorkflowEnvironment uses a virtual clock that skips real-time delays.
        // The 60s Workflow.sleep() should complete near-instantly in the test env.
        assertTrue(elapsed < 30_000,
            "TestWorkflowEnvironment should skip the 60s real-clock sleep; took " + elapsed + " ms");
    }
}
