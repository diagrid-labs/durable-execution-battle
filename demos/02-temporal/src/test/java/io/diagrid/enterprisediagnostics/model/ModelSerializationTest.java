package io.diagrid.enterprisediagnostics.model;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;

class ModelSerializationTest {

    private final ObjectMapper mapper = new ObjectMapper();

    @Test
    void diagnosticsInputRoundTrips() throws Exception {
        var input = new DiagnosticsInput(
            "enterprise-ncc-1701",
            "USS Enterprise NCC-1701-D",
            "2370-04-08",
            "Geordi La Forge");

        String json = mapper.writeValueAsString(input);
        assertThat(json).contains("\"id\":\"enterprise-ncc-1701\"");
        assertThat(json).contains("\"shipName\":\"USS Enterprise NCC-1701-D\"");

        DiagnosticsInput round = mapper.readValue(json, DiagnosticsInput.class);
        assertThat(round).isEqualTo(input);
    }

    @Test
    void scanResultRoundTrips() throws Exception {
        var result = new ScanResult("warp_core", 42, List.of("plasma conduit micro-fracture"));
        String json = mapper.writeValueAsString(result);

        assertThat(json).contains("\"subsystem\":\"warp_core\"");
        assertThat(json).contains("\"health\":42");
        assertThat(json).contains("\"anomalies\":[\"plasma conduit micro-fracture\"]");

        assertThat(mapper.readValue(json, ScanResult.class)).isEqualTo(result);
    }

    @Test
    void diagnosticsOutputAllowsNullBridgeNotification() throws Exception {
        var priority = new Priority(Severity.GREEN, "all nominal");
        var output = new DiagnosticsOutput(
            "USS Enterprise NCC-1701-D",
            "2370-04-08",
            "Geordi La Forge",
            priority,
            null);

        String json = mapper.writeValueAsString(output);
        assertThat(json).contains("\"bridgeNotification\":null");

        DiagnosticsOutput round = mapper.readValue(json, DiagnosticsOutput.class);
        assertThat(round.bridgeNotification()).isNull();
        assertThat(round.priority().severity()).isEqualTo(Severity.GREEN);
    }

    @Test
    void priorityUsesSnakeCaseSeverity() throws Exception {
        String json = mapper.writeValueAsString(new Priority(Severity.RED_ALERT, "critical"));
        assertThat(json).contains("\"severity\":\"red_alert\"");
        assertThat(json).contains("\"summary\":\"critical\"");
    }
}
