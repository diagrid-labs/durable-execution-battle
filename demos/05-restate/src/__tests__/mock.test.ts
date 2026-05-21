import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { generateScan, prioritize } from "../mock.ts";
import type { ScanResult } from "../models.ts";

describe("generateScan", () => {
  const candidates = ["alpha", "beta", "gamma", "delta", "epsilon"];

  it("returns the requested subsystem id verbatim", () => {
    const result = generateScan("warp_core", candidates);
    assert.equal(result.subsystem, "warp_core");
  });

  it("returns health in the rigged range [30, 65] inclusive", () => {
    for (let i = 0; i < 200; i++) {
      const result = generateScan("warp_core", candidates);
      assert.ok(result.health >= 30, `health ${result.health} < 30`);
      assert.ok(result.health <= 65, `health ${result.health} > 65`);
    }
  });

  it("returns 1 to 3 anomalies sampled from the candidate list", () => {
    for (let i = 0; i < 200; i++) {
      const result = generateScan("shields", candidates);
      assert.ok(result.anomalies.length >= 1, "must have at least 1 anomaly");
      assert.ok(result.anomalies.length <= 3, "must have at most 3 anomalies");
      for (const a of result.anomalies) {
        assert.ok(candidates.includes(a), `unexpected anomaly: ${a}`);
      }
    }
  });

  it("returns distinct anomalies (no duplicates in a single scan)", () => {
    for (let i = 0; i < 200; i++) {
      const result = generateScan("sensors", candidates);
      const unique = new Set(result.anomalies);
      assert.equal(unique.size, result.anomalies.length);
    }
  });
});

describe("prioritize", () => {
  function scan(subsystem: ScanResult["subsystem"], health: number): ScanResult {
    return { subsystem, health, anomalies: [] };
  }

  it("returns red_alert when the worst scan health is below 50", () => {
    const result = prioritize([
      scan("warp_core", 49),
      scan("shields", 80),
      scan("life_support", 90),
      scan("sensors", 70),
      scan("weapons", 60),
    ]);
    assert.equal(result.severity, "red_alert");
  });

  it("returns yellow_alert when the worst scan health is in [50, 75)", () => {
    const result = prioritize([
      scan("warp_core", 74),
      scan("shields", 80),
      scan("life_support", 90),
      scan("sensors", 76),
      scan("weapons", 85),
    ]);
    assert.equal(result.severity, "yellow_alert");
  });

  it("returns yellow_alert exactly at the lower edge of the band (50)", () => {
    const result = prioritize([scan("warp_core", 50)]);
    assert.equal(result.severity, "yellow_alert");
  });

  it("returns green when all scans are at or above 75", () => {
    const result = prioritize([
      scan("warp_core", 75),
      scan("shields", 95),
      scan("life_support", 88),
      scan("sensors", 80),
      scan("weapons", 90),
    ]);
    assert.equal(result.severity, "green");
  });

  it("names the worst subsystem in the summary", () => {
    const result = prioritize([
      scan("warp_core", 80),
      scan("shields", 32),
      scan("life_support", 90),
      scan("sensors", 76),
      scan("weapons", 60),
    ]);
    assert.match(result.summary, /shields/);
    assert.match(result.summary, /32/);
  });

  it("counts subsystems below the operational threshold (<75) in the summary", () => {
    const result = prioritize([
      scan("warp_core", 40),
      scan("shields", 70),
      scan("life_support", 90),
      scan("sensors", 60),
      scan("weapons", 80),
    ]);
    // worst=40, three subsystems below 75 (40, 70, 60)
    assert.match(result.summary, /3/);
  });
});
