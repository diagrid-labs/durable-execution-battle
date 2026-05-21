import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { formatBridgeAlert } from "../services.ts";

describe("formatBridgeAlert", () => {
  it("matches the canonical cross-demo format exactly", () => {
    const message = formatBridgeAlert({
      shipName: "USS Enterprise NCC-1701-D",
      engineerName: "Geordi La Forge",
      priority: {
        severity: "red_alert",
        summary: "warp_core critical at 32% health; 4 subsystem(s) below threshold",
      },
    });

    assert.equal(
      message,
      "🚨 BRIDGE ALERT — USS USS Enterprise NCC-1701-D — RED_ALERT: " +
        "warp_core critical at 32% health; 4 subsystem(s) below threshold " +
        "(engineer: Geordi La Forge)",
    );
  });

  it("uppercases the severity wire value", () => {
    const message = formatBridgeAlert({
      shipName: "X",
      engineerName: "Y",
      priority: { severity: "yellow_alert", summary: "Z" },
    });
    assert.match(message, /YELLOW_ALERT/);
  });
});
