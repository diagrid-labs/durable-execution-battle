import type { EngineAdapter } from "./types.ts";

export function createTemporal(baseUrl: string): EngineAdapter {
  return {
    name: "temporal",
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
      if (!res.ok) throw new Error(`temporal /start ${res.status}`);
      const body = (await res.json()) as { workflowId: string };
      return body.workflowId;
    },
    async isComplete(id) {
      const res = await fetch(`${baseUrl}/status/${encodeURIComponent(id)}`);
      if (!res.ok) return false;
      const body = (await res.json()) as { status: string };
      // Temporal returns protobuf enum names like "WORKFLOW_EXECUTION_STATUS_COMPLETED"
      // rather than the shorter "COMPLETED". Tolerate either form.
      return body.status === "WORKFLOW_EXECUTION_STATUS_COMPLETED" || body.status === "COMPLETED";
    },
  };
}
