// src/models.ts
//
// Wire-format invariants from plan.md §5.0 hold across all five demos:
//   - subsystem string values: warp_core, shields, life_support, sensors, weapons
//   - severity string values:  green, yellow_alert, red_alert
//   - severity thresholds:     min(health) < 50 → red_alert, < 75 → yellow_alert, else green
//   - health range:            [30..65] inclusive (rigged so red_alert is overwhelmingly likely)
//   - bridge alert format:     🚨 BRIDGE ALERT — USS {shipName} — {SEVERITY}: {summary} (engineer: {engineerName})
// TypeScript field names are camelCase per plan.md §5.0; only the *values* above stay snake_case.

export type Severity = "green" | "yellow_alert" | "red_alert";

export const Subsystem = {
  WarpCore:    "warp_core",
  Shields:     "shields",
  LifeSupport: "life_support",
  Sensors:     "sensors",
  Weapons:     "weapons",
} as const;
export type SubsystemId = typeof Subsystem[keyof typeof Subsystem];

export interface DiagnosticsInput {
  id: string;
  shipName: string;
  diagnosticsDate: string;
  engineerName: string;
}

export interface DiagnosticsOutput {
  shipName: string;
  diagnosticsDate: string;
  engineerName: string;
  priority: Priority;
  bridgeNotification: string | null;
}

export interface ScanInput {
  shipName: string;
  subsystem: SubsystemId;
}

export interface ScanResult {
  subsystem: SubsystemId;
  health: number;
  anomalies: string[];
}

export interface PrioritizeInput {
  shipName: string;
  scans: ScanResult[];
}

export interface Priority {
  severity: Severity;
  summary: string;
}

export interface BridgeNotificationInput {
  shipName: string;
  engineerName: string;
  priority: Priority;
}

export interface BridgeNotificationResult {
  message: string;
}
