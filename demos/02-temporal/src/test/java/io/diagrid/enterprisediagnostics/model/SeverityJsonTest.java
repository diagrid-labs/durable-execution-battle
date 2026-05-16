package io.diagrid.enterprisediagnostics.model;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.Test;

import static org.assertj.core.api.Assertions.assertThat;

class SeverityJsonTest {

    private final ObjectMapper mapper = new ObjectMapper();

    @Test
    void greenSerializesToLowercase() throws Exception {
        assertThat(mapper.writeValueAsString(Severity.GREEN)).isEqualTo("\"green\"");
    }

    @Test
    void yellowAlertSerializesToSnakeCase() throws Exception {
        assertThat(mapper.writeValueAsString(Severity.YELLOW_ALERT)).isEqualTo("\"yellow_alert\"");
    }

    @Test
    void redAlertSerializesToSnakeCase() throws Exception {
        assertThat(mapper.writeValueAsString(Severity.RED_ALERT)).isEqualTo("\"red_alert\"");
    }

    @Test
    void redAlertDeserializesFromSnakeCase() throws Exception {
        assertThat(mapper.readValue("\"red_alert\"", Severity.class)).isEqualTo(Severity.RED_ALERT);
    }

    @Test
    void yellowAlertDeserializesFromSnakeCase() throws Exception {
        assertThat(mapper.readValue("\"yellow_alert\"", Severity.class)).isEqualTo(Severity.YELLOW_ALERT);
    }
}
