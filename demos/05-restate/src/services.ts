// src/services.ts
//
// One Restate service named "activities" hosting all 7 handlers. Each handler
// is journaled as its own entry in the Restate UI; the workflow calls them
// via a typed service client (see diagnostics.ts).
//
// Non-determinism is allowed here — these functions run only when Restate
// invokes them, never during replay. The workflow stores their return values
// in the journal, so Math.random / Date.now produce stable replay results.

import * as restate from "@restatedev/restate-sdk";
import { generateScan, prioritize } from "./mock.ts";
import type {
  ScanInput,
  ScanResult,
  PrioritizeInput,
  Priority,
  BridgeNotificationInput,
  BridgeNotificationResult,
} from "./models.ts";

// ── Per-subsystem candidate anomaly lists (sampled by generateScan) ──

const warpCoreCandidates = [
  "plasma conduit micro-fracture (deck 36)",
  "warp coil phase variance 0.04%",
  "dilithium articulation frame torque drift",
  "matter/antimatter injector hesitation",
  "magnetic constriction segment 7 thermal creep",
];

const shieldsCandidates = [
  "shield grid harmonic decay (forward emitters)",
  "deflector dish calibration drift",
  "ablative armor micro-spalling",
  "structural integrity field fluctuation",
];

const lifeSupportCandidates = [
  "atmospheric scrubber CO2 saturation 0.6%",
  "deck 8 humidity sensor offline",
  "gravity plating phase variance",
  "ventilation manifold particulate buildup",
];

const sensorsCandidates = [
  "lateral sensor array calibration drift",
  "long-range subspace antenna polarization shift",
  "navigational deflector noise floor elevated",
  "tactical scanner refresh rate degraded",
];

const weaponsCandidates = [
  "phaser bank 3 thermal coupling intermittent",
  "torpedo loader servo lag",
  "targeting array micro-latency",
  "EPS conduit to weapons grid showing micro-arcs",
];

// Exported for unit testing — keeps the alert template in one place and
// lets tests pin the exact cross-demo format without booting Restate.
export function formatBridgeAlert(input: BridgeNotificationInput): string {
  return (
    `🚨 BRIDGE ALERT — USS ${input.shipName} — ` +
    `${input.priority.severity.toUpperCase()}: ${input.priority.summary} ` +
    `(engineer: ${input.engineerName})`
  );
}

export const activities = restate.service({
  name: "activities",
  handlers: {
    scanWarpCore: async (
      _ctx: restate.Context,
      _input: ScanInput,
    ): Promise<ScanResult> => generateScan("warp_core", warpCoreCandidates),

    scanShields: async (
      _ctx: restate.Context,
      _input: ScanInput,
    ): Promise<ScanResult> => generateScan("shields", shieldsCandidates),

    scanLifeSupport: async (
      _ctx: restate.Context,
      _input: ScanInput,
    ): Promise<ScanResult> => generateScan("life_support", lifeSupportCandidates),

    scanSensors: async (
      _ctx: restate.Context,
      _input: ScanInput,
    ): Promise<ScanResult> => generateScan("sensors", sensorsCandidates),

    scanWeapons: async (
      _ctx: restate.Context,
      _input: ScanInput,
    ): Promise<ScanResult> => generateScan("weapons", weaponsCandidates),

    prioritizeIssues: async (
      _ctx: restate.Context,
      input: PrioritizeInput,
    ): Promise<Priority> => prioritize(input.scans),

    notifyBridge: async (
      _ctx: restate.Context,
      input: BridgeNotificationInput,
    ): Promise<BridgeNotificationResult> => {
      const message = formatBridgeAlert(input);
      console.warn(message);
      return { message };
    },
  },
});
