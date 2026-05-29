import type { WorkflowRow } from "./stats.ts";

const HEADER = "id,t_start_ms,t_end_ms,latency_ms,status";

export function rowsToCsv(rows: WorkflowRow[]): string {
  const lines = rows.map(
    (r) => `${r.id},${r.t_start_ms},${r.t_end_ms},${r.latency_ms},${r.status}`,
  );
  return [HEADER, ...lines, ""].join("\n");
}
