import type { EngineAdapter } from "./types.ts";

export function createRestate(baseUrl: string): EngineAdapter {
  return {
    name: "restate",
    baseUrl,
    buildInput(id: string) {
      return {
        id,
        shipName: "Enterprise",
        diagnosticsDate: "2026-05-28",
        engineerName: "perf-driver",
        testing: true,
      };
    },
    async start(input) {
      // POST /start on the demo's Express API, which calls Restate's workflowSubmit
      // and returns 202 with { workflowId: input.id }.
      const res = await fetch(`${baseUrl}/start`, {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify(input),
      });
      if (!res.ok) throw new Error(`restate /start ${res.status}`);
      const body = (await res.json()) as { workflowId: string };
      return body.workflowId;
    },
    async isComplete(id) {
      // GET /status/:id on the demo's Express API returns:
      //   { status: "COMPLETED", output: {...} } when done
      //   { status: "RUNNING" } (HTTP 202) while in flight
      // We treat any non-ok status as "not yet complete".
      const res = await fetch(`${baseUrl}/status/${encodeURIComponent(id)}`);
      if (!res.ok) return false;
      const body = (await res.json()) as { status: string };
      return body.status === "COMPLETED";
    },
  };
}
