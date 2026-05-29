package io.diagrid.enterprisediagnostics.model;

public record DiagnosticsInput(
    String id,
    String shipName,
    String diagnosticsDate,
    String engineerName,
    boolean testing) {

    public DiagnosticsInput(String id, String shipName, String diagnosticsDate, String engineerName) {
        this(id, shipName, diagnosticsDate, engineerName, false);
    }
}
