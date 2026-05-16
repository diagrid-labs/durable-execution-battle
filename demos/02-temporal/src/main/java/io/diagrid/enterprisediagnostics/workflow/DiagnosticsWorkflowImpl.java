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

        // KILL POINT — durable timer
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
