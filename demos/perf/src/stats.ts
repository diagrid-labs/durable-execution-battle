export type WorkflowRowStatus = "COMPLETED" | "TIMEOUT";

export interface WorkflowRow {
  id: string;
  t_start_ms: number;
  t_end_ms: number;
  latency_ms: number;
  status: WorkflowRowStatus;
}

export interface Summary {
  n: number;
  completed: number;
  errors: number;
  wallclock_ms: number;
  throughput_wps: number;
  p50_ms: number;
  p95_ms: number;
  p99_ms: number;
}

export function percentile(values: number[], p: number): number {
  if (values.length === 0) throw new Error("percentile of empty input");
  const sorted = [...values].sort((a, b) => a - b);
  if (p <= 0) return sorted[0];
  if (p >= 100) return sorted[sorted.length - 1];
  // Hyndman & Fan Type 6: h = (p/100) * (n+1), 1-indexed
  const h = (p / 100) * (sorted.length + 1);
  const lo = Math.floor(h) - 1; // convert to 0-based
  const hi = Math.ceil(h) - 1;
  if (lo < 0) return sorted[0];
  if (hi >= sorted.length) return sorted[sorted.length - 1];
  if (lo === hi) return sorted[lo];
  const frac = h - Math.floor(h);
  return sorted[lo] * (1 - frac) + sorted[hi] * frac;
}

export function summarize(rows: WorkflowRow[], wallclock_ms: number): Summary {
  const completed = rows.filter((r) => r.status === "COMPLETED");
  const errors = rows.length - completed.length;
  const latencies = completed.map((r) => r.latency_ms);
  const throughput_wps = wallclock_ms > 0 ? (completed.length / wallclock_ms) * 1000 : 0;
  return {
    n: rows.length,
    completed: completed.length,
    errors,
    wallclock_ms,
    throughput_wps,
    p50_ms: latencies.length ? percentile(latencies, 50) : 0,
    p95_ms: latencies.length ? percentile(latencies, 95) : 0,
    p99_ms: latencies.length ? percentile(latencies, 99) : 0,
  };
}
