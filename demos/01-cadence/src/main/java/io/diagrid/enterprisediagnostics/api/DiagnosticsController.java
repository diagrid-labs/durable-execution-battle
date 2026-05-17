package io.diagrid.enterprisediagnostics.api;

import com.uber.cadence.TerminateWorkflowExecutionRequest;
import com.uber.cadence.WorkflowExecution;
import com.uber.cadence.client.WorkflowClient;
import com.uber.cadence.client.WorkflowOptions;
import com.uber.cadence.client.WorkflowStub;
import io.diagrid.enterprisediagnostics.config.CadenceProperties;
import io.diagrid.enterprisediagnostics.model.DiagnosticsInput;
import io.diagrid.enterprisediagnostics.model.DiagnosticsOutput;
import io.diagrid.enterprisediagnostics.workflow.DiagnosticsWorkflow;
import org.springframework.web.bind.annotation.*;

import java.util.Map;
import java.util.Optional;
import java.util.concurrent.TimeUnit;

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
                .setExecutionStartToCloseTimeout(java.time.Duration.ofMinutes(10))
                .build());

        WorkflowClient.start(stub::runDiagnostics, input);
        return Map.of("workflowId", input.id());
    }

    @GetMapping("/status/{id}")
    public Map<String, Object> status(@PathVariable String id) {
        WorkflowStub stub = client.newUntypedWorkflowStub(id, Optional.empty(), Optional.empty());
        try {
            DiagnosticsOutput result = stub.getResult(2, TimeUnit.SECONDS, DiagnosticsOutput.class);
            return Map.of("status", "COMPLETED", "output", result);
        } catch (java.util.concurrent.TimeoutException e) {
            return Map.of("status", "RUNNING", "detail", "TimeoutException");
        } catch (Exception e) {
            return Map.of("status", "RUNNING_OR_FAILED", "detail", e.getClass().getSimpleName(),
                "message", e.getMessage() != null ? e.getMessage() : "null");
        }
    }

    @PostMapping("/terminate/{id}")
    public Map<String, String> terminate(@PathVariable String id) {
        TerminateWorkflowExecutionRequest request = new TerminateWorkflowExecutionRequest();
        request.setDomain(props.domain());
        WorkflowExecution execution = new WorkflowExecution();
        execution.setWorkflowId(id);
        request.setWorkflowExecution(execution);
        request.setReason("manual termination via /terminate endpoint");

        try {
            client.getService().TerminateWorkflowExecution(request);
        } catch (Exception e) {
            return Map.of("workflowId", id, "terminated", "false", "error", e.getClass().getSimpleName());
        }
        return Map.of("workflowId", id, "terminated", "true");
    }

    @PostMapping("/cancel/{id}")
    public Map<String, String> cancel(@PathVariable String id) {
        WorkflowStub stub = client.newUntypedWorkflowStub(id, Optional.empty(), Optional.empty());
        stub.cancel();
        return Map.of("workflowId", id, "cancelled", "true");
    }
}
