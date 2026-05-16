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
import java.util.concurrent.atomic.AtomicInteger;

import static org.assertj.core.api.Assertions.assertThat;

class DiagnosticsWorkflowImplTest {

    private TestWorkflowEnvironment env;
    private WorkflowClient client;

    // Counters to verify invocations without Mockito
    private final AtomicInteger warpCount       = new AtomicInteger();
    private final AtomicInteger shieldsCount    = new AtomicInteger();
    private final AtomicInteger lifeCount       = new AtomicInteger();
    private final AtomicInteger sensorsCount    = new AtomicInteger();
    private final AtomicInteger weaponsCount    = new AtomicInteger();
    private final AtomicInteger prioritizeCount = new AtomicInteger();
    private final AtomicInteger notifyCount     = new AtomicInteger();

    // Configurable return values
    private ScanResult warpResult;
    private ScanResult shieldsResult;
    private ScanResult lifeResult;
    private ScanResult sensorsResult;
    private ScanResult weaponsResult;
    private Priority priorityResult;
    private BridgeNotificationResult notifyResult;

    @BeforeEach
    void setUp() {
        env = TestWorkflowEnvironment.newInstance();
        Worker worker = env.newWorker("diagnostics-tq");
        worker.registerWorkflowImplementationTypes(DiagnosticsWorkflowImpl.class);

        worker.registerActivitiesImplementations(
            (ScanWarpCoreActivity)    input -> { warpCount.incrementAndGet();    return warpResult;     },
            (ScanShieldsActivity)     input -> { shieldsCount.incrementAndGet(); return shieldsResult;  },
            (ScanLifeSupportActivity) input -> { lifeCount.incrementAndGet();    return lifeResult;     },
            (ScanSensorsActivity)     input -> { sensorsCount.incrementAndGet(); return sensorsResult;  },
            (ScanWeaponsActivity)     input -> { weaponsCount.incrementAndGet(); return weaponsResult;  },
            (PrioritizeIssuesActivity)input -> { prioritizeCount.incrementAndGet(); return priorityResult; },
            (NotifyBridgeActivity)    input -> { notifyCount.incrementAndGet();  return notifyResult;   }
        );

        env.start();
        client = env.getWorkflowClient();
    }

    @AfterEach
    void tearDown() {
        env.close();
        warpCount.set(0); shieldsCount.set(0); lifeCount.set(0);
        sensorsCount.set(0); weaponsCount.set(0);
        prioritizeCount.set(0); notifyCount.set(0);
    }

    @Test
    void redAlertPathInvokesAllFiveScansThenPrioritizeThenNotify() {
        warpResult    = new ScanResult("warp_core",    40, List.of("a"));
        shieldsResult = new ScanResult("shields",      80, List.of("b"));
        lifeResult    = new ScanResult("life_support", 90, List.of("c"));
        sensorsResult = new ScanResult("sensors",      85, List.of("d"));
        weaponsResult = new ScanResult("weapons",      95, List.of("e"));
        priorityResult = new Priority(Severity.RED_ALERT, "warp_core critical");
        notifyResult   = new BridgeNotificationResult("BRIDGE ALERT message");

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

        assertThat(warpCount.get()).isEqualTo(1);
        assertThat(shieldsCount.get()).isEqualTo(1);
        assertThat(lifeCount.get()).isEqualTo(1);
        assertThat(sensorsCount.get()).isEqualTo(1);
        assertThat(weaponsCount.get()).isEqualTo(1);
        assertThat(prioritizeCount.get()).isEqualTo(1);
        assertThat(notifyCount.get()).isEqualTo(1);
    }

    @Test
    void greenPathSkipsNotify() {
        warpResult    = new ScanResult("warp_core",    90, List.of("a"));
        shieldsResult = new ScanResult("shields",      95, List.of("b"));
        lifeResult    = new ScanResult("life_support", 92, List.of("c"));
        sensorsResult = new ScanResult("sensors",      88, List.of("d"));
        weaponsResult = new ScanResult("weapons",      85, List.of("e"));
        priorityResult = new Priority(Severity.GREEN, "all nominal");
        notifyResult   = null;

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
        assertThat(notifyCount.get()).isEqualTo(0);
    }
}
