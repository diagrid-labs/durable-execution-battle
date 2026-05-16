package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.PrioritizeInput;
import io.diagrid.enterprisediagnostics.model.Priority;
import io.temporal.activity.ActivityInterface;
import io.temporal.activity.ActivityMethod;

@ActivityInterface
public interface PrioritizeIssuesActivity {
    @ActivityMethod(name = "PrioritizeIssuesActivity")
    Priority execute(PrioritizeInput input);
}
