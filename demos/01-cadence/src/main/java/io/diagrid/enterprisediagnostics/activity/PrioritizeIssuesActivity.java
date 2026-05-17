package io.diagrid.enterprisediagnostics.activity;

import com.uber.cadence.activity.ActivityMethod;
import io.diagrid.enterprisediagnostics.model.PrioritizeInput;
import io.diagrid.enterprisediagnostics.model.Priority;

public interface PrioritizeIssuesActivity {
    @ActivityMethod
    Priority execute(PrioritizeInput input);
}
