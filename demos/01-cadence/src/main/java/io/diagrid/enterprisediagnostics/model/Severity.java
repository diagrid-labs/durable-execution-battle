package io.diagrid.enterprisediagnostics.model;

import com.fasterxml.jackson.annotation.JsonProperty;

public enum Severity {
    @JsonProperty("green")        GREEN,
    @JsonProperty("yellow_alert") YELLOW_ALERT,
    @JsonProperty("red_alert")    RED_ALERT;
}
