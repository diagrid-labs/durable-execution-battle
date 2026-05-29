import type { EngineAdapter } from "./types.ts";

export function createCadence(baseUrl: string): EngineAdapter {
  return {
    name: "cadence",
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
      const res = await fetch(`${baseUrl}/start`, {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify(input),
      });
      if (!res.ok) throw new Error(`cadence /start ${res.status}`);
      const body = (await res.json()) as { workflowId: string };
      return body.workflowId;
    },
    async isComplete(id) {
      const res = await fetch(`${baseUrl}/status/${encodeURIComponent(id)}`);
      if (!res.ok) return false;
      const body = (await res.json()) as { status: string };
      return body.status === "COMPLETED";
    },
  };
}
