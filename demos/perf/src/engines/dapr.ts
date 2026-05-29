import type { EngineAdapter } from "./types.ts";

export function createDapr(baseUrl: string): EngineAdapter {
  return {
    name: "dapr",
    baseUrl,
    buildInput(id: string) {
      // Dapr's C# record uses PascalCase property names (Id, ShipName, etc.).
      // ASP.NET System.Text.Json defaults to camelCase on the wire, and
      // PropertyNameCaseInsensitive is typically true, but PascalCase is used
      // here to exactly match the record definition.
      return {
        Id: id,
        ShipName: "Enterprise",
        DiagnosticsDate: "2026-05-28",
        EngineerName: "perf-driver",
        Testing: true,
      };
    },
    async start(input) {
      const res = await fetch(`${baseUrl}/start`, {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify(input),
      });
      if (!res.ok) throw new Error(`dapr /start ${res.status}`);
      // Response is Results.Ok(new { instanceId }) — serializes as { "instanceId": "..." }
      const body = (await res.json()) as { instanceId: string };
      return body.instanceId;
    },
    async isComplete(id) {
      // Dapr's /status/{id} returns 404 until the workflow exists; treat that as
      // "not yet complete" rather than an error.
      const res = await fetch(`${baseUrl}/status/${encodeURIComponent(id)}`);
      if (res.status === 404) return false;
      if (!res.ok) return false;
      // Response is Results.Ok(new { state, output }) where state is WorkflowState.
      // WorkflowRuntimeStatus.Completed = 1 (enum int). The Dapr demo's Program.cs
      // does not register JsonStringEnumConverter, so on the wire runtimeStatus
      // arrives as the integer 1 rather than a string. Accept both forms for
      // robustness against a future Program.cs change.
      const body = (await res.json()) as {
        state?: { runtimeStatus?: string | number };
      };
      const rs = body.state?.runtimeStatus;
      return rs === 1 || rs === "COMPLETED" || rs === "Completed";
    },
  };
}
