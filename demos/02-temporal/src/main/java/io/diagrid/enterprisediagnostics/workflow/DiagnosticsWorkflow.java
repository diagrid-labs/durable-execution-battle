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
