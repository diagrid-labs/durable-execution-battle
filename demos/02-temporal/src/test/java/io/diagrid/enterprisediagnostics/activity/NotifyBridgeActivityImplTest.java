package io.diagrid.enterprisediagnostics.activity;

import io.diagrid.enterprisediagnostics.model.BridgeNotificationInput;
import io.diagrid.enterprisediagnostics.model.BridgeNotificationResult;
import io.diagrid.enterprisediagnostics.model.Priority;
import io.diagrid.enterprisediagnostics.model.Severity;
import org.junit.jupiter.api.Test;

import static org.assertj.core.api.Assertions.assertThat;

class NotifyBridgeActivityImplTest {

    private final NotifyBridgeActivity activity = new NotifyBridgeActivityImpl();

    @Test
    void messageFollowsCanonicalFormat() {
        var priority = new Priority(Severity.RED_ALERT, "warp core critical at 40% health");
        var input = new BridgeNotificationInput(
            "USS Enterprise NCC-1701-D", "Geordi La Forge", priority);

        BridgeNotificationResult result = activity.execute(input);

        assertThat(result.message()).isEqualTo(
            "🚨 BRIDGE ALERT — USS Enterprise NCC-1701-D — RED_ALERT: " +
            "warp core critical at 40% health (engineer: Geordi La Forge)");
    }

    @Test
    void severityIsUppercasedInMessage() {
        var priority = new Priority(Severity.YELLOW_ALERT, "minor issue");
        var input = new BridgeNotificationInput("Voyager", "B'Elanna", priority);

        assertThat(activity.execute(input).message()).contains("YELLOW_ALERT");
    }
}
