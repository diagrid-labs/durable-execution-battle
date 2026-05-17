package io.diagrid.enterprisediagnostics.model;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.assertEquals;

class SeverityJsonTest {

    private final ObjectMapper mapper = new ObjectMapper();

    @Test
    void serializesGreenAsLowercase() throws Exception {
        assertEquals("\"green\"", mapper.writeValueAsString(Severity.GREEN));
    }

    @Test
    void serializesYellowAlertAsSnakeCase() throws Exception {
        assertEquals("\"yellow_alert\"", mapper.writeValueAsString(Severity.YELLOW_ALERT));
    }

    @Test
    void serializesRedAlertAsSnakeCase() throws Exception {
        assertEquals("\"red_alert\"", mapper.writeValueAsString(Severity.RED_ALERT));
    }

    @Test
    void deserializesGreenFromLowercase() throws Exception {
        assertEquals(Severity.GREEN, mapper.readValue("\"green\"", Severity.class));
    }

    @Test
    void deserializesYellowAlertFromSnakeCase() throws Exception {
        assertEquals(Severity.YELLOW_ALERT, mapper.readValue("\"yellow_alert\"", Severity.class));
    }

    @Test
    void deserializesRedAlertFromSnakeCase() throws Exception {
        assertEquals(Severity.RED_ALERT, mapper.readValue("\"red_alert\"", Severity.class));
    }
}
