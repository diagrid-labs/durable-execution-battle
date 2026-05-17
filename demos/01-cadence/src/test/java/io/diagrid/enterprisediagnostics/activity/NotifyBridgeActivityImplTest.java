package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.BridgeNotificationInput;
import io.diagrid.enterprisediagnostics.model.BridgeNotificationResult;
import io.diagrid.enterprisediagnostics.model.Priority;
import io.diagrid.enterprisediagnostics.model.Severity;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.*;

class NotifyBridgeActivityImplTest {

    private final NotifyBridgeActivity activity = new NotifyBridgeActivityImpl();

    @Test
    void messageFormatMatchesCanonicalString() {
        BridgeNotificationResult r = activity.execute(new BridgeNotificationInput(
            "USS Enterprise NCC-1701-D",
            "Geordi La Forge",
            new Priority(Severity.RED_ALERT, "warp_core critical at 40% health; 2 subsystem(s) below threshold")));

        assertEquals(
            "🚨 BRIDGE ALERT — USS USS Enterprise NCC-1701-D — RED_ALERT: warp_core critical at 40% health; 2 subsystem(s) below threshold (engineer: Geordi La Forge)",
            r.message());
    }

    @Test
    void usesEnumNameNotJsonValueForOnStageReadability() {
        BridgeNotificationResult r = activity.execute(new BridgeNotificationInput(
            "Voyager",
            "B'Elanna",
            new Priority(Severity.YELLOW_ALERT, "shields at 70% health; 1 subsystem(s) below threshold")));

        assertTrue(r.message().contains("YELLOW_ALERT"),
            "expected enum.name() in message for visibility; got: " + r.message());
    }
}
