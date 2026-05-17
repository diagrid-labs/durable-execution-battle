package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.BridgeNotificationInput;
import io.diagrid.enterprisediagnostics.model.BridgeNotificationResult;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

@Component
public class NotifyBridgeActivityImpl implements NotifyBridgeActivity {

    private static final Logger log = LoggerFactory.getLogger(NotifyBridgeActivityImpl.class);

    @Override
    public BridgeNotificationResult execute(BridgeNotificationInput input) {
        String message = "🚨 BRIDGE ALERT — USS %s — %s: %s (engineer: %s)".formatted(
            input.shipName(),
            input.priority().severity().name(),
            input.priority().summary(),
            input.engineerName());
        log.warn(message);
        return new BridgeNotificationResult(message);
    }
}
