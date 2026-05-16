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
