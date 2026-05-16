package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import io.temporal.activity.ActivityInterface;
import io.temporal.activity.ActivityMethod;

@ActivityInterface
public interface ScanSensorsActivity {
    @ActivityMethod(name = "ScanSensorsActivity")
    ScanResult execute(ScanInput input);
}
