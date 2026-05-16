package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.ScanInput;
import io.diagrid.enterprisediagnostics.model.ScanResult;
import io.temporal.activity.ActivityInterface;
import io.temporal.activity.ActivityMethod;

@ActivityInterface
public interface ScanWeaponsActivity {
    @ActivityMethod(name = "ScanWeaponsActivity")
    ScanResult execute(ScanInput input);
}
