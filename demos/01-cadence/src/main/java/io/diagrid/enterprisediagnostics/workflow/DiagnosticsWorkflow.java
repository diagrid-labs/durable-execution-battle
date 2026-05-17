package io.diagrid.enterprisediagnostics.workflow;

import com.uber.cadence.workflow.WorkflowMethod;
import io.diagrid.enterprisediagnostics.model.DiagnosticsInput;
import io.diagrid.enterprisediagnostics.model.DiagnosticsOutput;

public interface DiagnosticsWorkflow {
    @WorkflowMethod
    DiagnosticsOutput runDiagnostics(DiagnosticsInput input);
}
