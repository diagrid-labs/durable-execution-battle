package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.BridgeNotificationInput;
import io.diagrid.enterprisediagnostics.model.BridgeNotificationResult;
import io.temporal.spring.boot.ActivityImpl;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

@Component
@ActivityImpl(taskQueues = "diagnostics-tq")
public class NotifyBridgeActivityImpl implements NotifyBridgeActivity {

    private static final Logger log = LoggerFactory.getLogger(NotifyBridgeActivityImpl.class);

    @Override
    public BridgeNotificationResult execute(BridgeNotificationInput input) {
        String message = "🚨 BRIDGE ALERT — %s — %s: %s (engineer: %s)".formatted(
            input.shipName(),
            input.priority().severity().name(),
            input.priority().summary(),
            input.engineerName());
        log.warn(message);
        return new BridgeNotificationResult(message);
    }
}
