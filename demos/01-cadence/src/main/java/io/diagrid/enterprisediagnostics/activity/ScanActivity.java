package io.diagrid.enterprisediagnostics.activity;

/**
 * Marker interface implemented by all five Scan*ActivityImpl classes so that
 * Spring can inject the full set as List<ScanActivity> into CadenceConfig.
 * Not annotated with @ActivityMethod — the concrete sub-interfaces carry that.
 */
public interface ScanActivity {}
