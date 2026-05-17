package io.diagrid.enterprisediagnostics.activity;

import com.uber.cadence.activity.ActivityMethod;
import io.diagrid.enterprisediagnostics.model.BridgeNotificationInput;
import io.diagrid.enterprisediagnostics.model.BridgeNotificationResult;

public interface NotifyBridgeActivity {
    @ActivityMethod
    BridgeNotificationResult execute(BridgeNotificationInput input);
}
