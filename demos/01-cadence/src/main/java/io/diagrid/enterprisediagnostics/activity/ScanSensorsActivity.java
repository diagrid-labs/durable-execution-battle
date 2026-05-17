package io.diagrid.enterprisediagnostics.activity;

import com.uber.cadence.activity.ActivityMethod;
import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;

public interface ScanSensorsActivity {
    @ActivityMethod
    ScanResult execute(ScanInput input);
}
