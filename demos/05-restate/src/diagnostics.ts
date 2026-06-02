// src/diagnostics.ts
//
// Workflow body MUST be deterministic. Restate replays this handler on every
// re-invocation (e.g. after a kill -9 of the Node process during the 60s
// sleep). Forbidden inside this function: Math.random, Date.now, plain
// console.log, file/network I/O, any non-journaled side-effect.
// Allowed: service-client calls (each is one journal entry), ctx.sleep,
// ctx.console, ctx.run.

import * as restate from "@restatedev/restate-sdk";
import { activities } from "./services.ts";
import type {
  DiagnosticsInput,
  DiagnosticsOutput,
  ScanResult,
  Priority,
} from "./models.ts";

export const diagnosticsWorkflow = restate.workflow({
  name: "DiagnosticsWorkflow",
  handlers: {
    run: async (
      ctx: restate.WorkflowContext,
      input: DiagnosticsInput,
    ): Promise<DiagnosticsOutput> => {
      const client = ctx.serviceClient(activities);

      ctx.console.log(
        `Starting diagnostics for ${input.shipName} (id: ${input.id})`,
      );

      // ── Fan-out: 5 parallel subsystem scans ──
      const scans: ScanResult[] = await Promise.all([
        client.scanWarpCore({ shipName: input.shipName, subsystem: "warp_core" }),
        client.scanShields({ shipName: input.shipName, subsystem: "shields" }),
        client.scanLifeSupport({ shipName: input.shipName, subsystem: "life_support" }),
        client.scanSensors({ shipName: input.shipName, subsystem: "sensors" }),
        client.scanWeapons({ shipName: input.shipName, subsystem: "weapons" }),
      ]);

      if (!input.testing) {
        ctx.console.log("Engineering review pending — sleeping 60s");
        // ── 60s durable sleep — ⚡ KILL POINT ⚡ ──
        await ctx.sleep({ seconds: 60 });
      }

      // ── Prioritization ──
      const priority: Priority = await client.prioritizeIssues({
        shipName: input.shipName,
        scans,
      });

      // ── Conditional bridge notification ──
      let bridgeNotification: string | null = null;
      if (priority.severity === "red_alert" || priority.severity === "yellow_alert") {
        const result = await client.notifyBridge({
          shipName: input.shipName,
          engineerName: input.engineerName,
          priority,
        });
        bridgeNotification = result.message;
      }

      ctx.console.log(`Diagnostics complete for ${input.shipName} (id: ${input.id})`);

      return {
        shipName: input.shipName,
        diagnosticsDate: input.diagnosticsDate,
        engineerName: input.engineerName,
        priority,
        bridgeNotification,
      };
    },
  },
});
