import type { EngineAdapter } from "./types.ts";

export function createConductor(baseUrl: string): EngineAdapter {
  return {
    name: "conductor",
    baseUrl,
    buildInput(id: string) {
      // Conductor's ASP.NET API uses JsonNamingPolicy.SnakeCaseLower on the wire.
      // PropertyNameCaseInsensitive = true means camelCase is also accepted on input,
      // but snake_case is used here to match the .http file exactly.
      return {
        id,
        ship_name: "Enterprise",
        diagnostics_date: "2026-05-28",
        engineer_name: "perf-driver",
        testing: true,
      };
    },
    async start(input) {
      const res = await fetch(`${baseUrl}/start`, {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify(input),
      });
      if (!res.ok) throw new Error(`conductor /start ${res.status}`);
      // The global SnakeCaseLower policy serializes { workflowId } as { workflow_id }.
      // Confirmed by the .http file: response.body.$.workflow_id
      const body = (await res.json()) as {
        workflow_id?: string;
        workflowId?: string;
      };
      const wfId = body.workflow_id ?? body.workflowId;
      if (!wfId) throw new Error("conductor /start: no workflow_id in response");
      return wfId;
    },
    async isComplete(id) {
      const res = await fetch(`${baseUrl}/status/${encodeURIComponent(id)}`);
      if (!res.ok) return false;
      // Status is serialized via Newtonsoft (not the ASP.NET policy), so field
      // name stays lowercase "status" and value is the Conductor execution status
      // string (e.g. "COMPLETED", "RUNNING").
      const body = (await res.json()) as { status: string };
      return body.status === "COMPLETED";
    },
  };
}
