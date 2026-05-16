package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.BridgeNotificationInput;
import io.diagrid.enterprisediagnostics.model.BridgeNotificationResult;
import io.temporal.activity.ActivityInterface;
import io.temporal.activity.ActivityMethod;

@ActivityInterface
public interface NotifyBridgeActivity {
    @ActivityMethod(name = "NotifyBridgeActivity")
    BridgeNotificationResult execute(BridgeNotificationInput input);
}
