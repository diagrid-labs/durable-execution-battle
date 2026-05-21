// src/api.ts
//
// Thin Express wrapper. The audience POSTs JSON here; we forward to Restate's
// ingress (default :8080) using the typed clients SDK. No durable logic
// lives in this file — it's pure glue so the on-stage .http file shape
// matches Cadence/Temporal/Conductor/Dapr.

import express from "express";
import * as clients from "@restatedev/restate-sdk-clients";
import { diagnosticsWorkflow } from "./diagnostics.ts";
import type { DiagnosticsInput } from "./models.ts";

const RESTATE_INGRESS = process.env.RESTATE_INGRESS ?? "http://localhost:8080";
const PORT = Number(process.env.PORT ?? 3000);

export function buildApi() {
  const app = express();
  app.use(express.json());

  const restateClient = clients.connect({ url: RESTATE_INGRESS });

  // POST /start — kick off a diagnostics workflow.
  // Returns 202 immediately with the workflow id; the actual run takes 60s+.
  app.post("/start", async (req, res) => {
    const input = req.body as DiagnosticsInput;
    if (!input?.id || !input?.shipName) {
      res.status(400).json({ error: "id and shipName are required" });
      return;
    }
    try {
      const handle = restateClient.workflowClient(diagnosticsWorkflow, input.id);
      await handle.workflowSubmit(input);
      res.status(202).json({ workflowId: input.id });
    } catch (err) {
      console.error("Failed to start workflow", err);
      res.status(500).json({ error: String(err) });
    }
  });

  // GET /status/:id — if the workflow has completed, return its output;
  // otherwise return 202 with {status: "RUNNING"}.
  app.get("/status/:id", async (req, res) => {
    const id = req.params.id;
    try {
      const handle = restateClient.workflowClient(diagnosticsWorkflow, id);
      const output = await Promise.race([
        handle.workflowOutput(),
        new Promise<never>((_, rej) =>
          setTimeout(() => rej(new Error("pending")), 250),
        ),
      ]);
      if (!output.ready) {
        res.status(202).json({ status: "RUNNING" });
        return;
      }
      res.json({ status: "COMPLETED", output: output.result });
    } catch {
      res.status(202).json({ status: "RUNNING" });
    }
  });

  return app.listen(PORT, () =>
    console.log(`Express API listening on http://localhost:${PORT}`),
  );
}
