import type { WorkflowRow, WorkflowRowStatus } from "./stats.ts";

const HEADER = "id,t_start_ms,t_end_ms,latency_ms,status";

export function rowsToCsv(rows: WorkflowRow[]): string {
  const lines = rows.map(
    (r) => `${r.id},${r.t_start_ms},${r.t_end_ms},${r.latency_ms},${r.status}`,
  );
  return [HEADER, ...lines, ""].join("\n");
}

export function parseCsv(text: string): WorkflowRow[] {
  const lines = text.split("\n").map((l) => l.trim()).filter((l) => l.length > 0);
  const body = lines[0] === HEADER ? lines.slice(1) : lines;
  return body.map((line) => {
    const [id, t_start_ms, t_end_ms, latency_ms, status] = line.split(",");
    return {
      id,
      t_start_ms: Number(t_start_ms),
      t_end_ms: Number(t_end_ms),
      latency_ms: Number(latency_ms),
      status: status as WorkflowRowStatus,
    };
  });
}
