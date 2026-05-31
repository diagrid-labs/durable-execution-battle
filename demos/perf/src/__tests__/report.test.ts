import { describe, it, expect } from "vitest";
import { buildReportData } from "../report.ts";
import type { WorkflowRow } from "../stats.ts";

function row(id: string, latency: number, status: "COMPLETED" | "TIMEOUT" = "COMPLETED"): WorkflowRow {
  return { id, t_start_ms: 0, t_end_ms: latency, latency_ms: latency, status };
}

describe("buildReportData", () => {
  it("returns an empty array for no files", () => {
    expect(buildReportData([])).toEqual([]);
  });

  it("groups files by engine prefix and orders engines by the fixed display order", () => {
    const data = buildReportData([
      { name: "restate-100-2026-05-29T10-00-00-000Z.csv", rows: [row("a", 100)] },
      { name: "dapr-100-2026-05-29T10-00-00-000Z.csv", rows: [row("a", 100)] },
      { name: "conductor-100-2026-05-29T10-00-00-000Z.csv", rows: [row("a", 100)] },
      { name: "temporal-100-2026-05-29T10-00-00-000Z.csv", rows: [row("a", 100)] },
      { name: "cadence-100-2026-05-29T10-00-00-000Z.csv", rows: [row("a", 100)] },
    ]);
    expect(data.map((e) => e.engine)).toEqual([
      "cadence",
      "temporal",
      "conductor",
      "dapr",
      "restate",
    ]);
  });

  it("labels runs Run 1..N in filename (timestamp) order", () => {
    const data = buildReportData([
      { name: "dapr-100-2026-05-29T18-00-00-000Z.csv", rows: [row("a", 100)] },
      { name: "dapr-100-2026-05-29T10-00-00-000Z.csv", rows: [row("a", 100)] },
    ]);
    const dapr = data.find((e) => e.engine === "dapr")!;
    expect(dapr.runs.map((r) => r.label)).toEqual(["Run 1", "Run 2"]);
    expect(dapr.runs[0].source).toContain("T10-00-00");
  });

  it("computes per-run percentiles over COMPLETED rows only and counts errors", () => {
    const data = buildReportData([
      {
        name: "restate-100-2026-05-29T10-00-00-000Z.csv",
        rows: [row("a", 100), row("b", 300), row("c", 500), row("d", 900, "TIMEOUT")],
      },
    ]);
    const run = data[0].runs[0];
    expect(run.completed).toBe(3);
    expect(run.errors).toBe(1);
    expect(run.latencies).toEqual([100, 300, 500]);
    expect(run.p50).toBe(300);
  });

  it("keeps the original workflow order in sequence, with null for non-COMPLETED rows", () => {
    const data = buildReportData([
      {
        name: "restate-100-2026-05-29T10-00-00-000Z.csv",
        rows: [row("a", 100), row("b", 900, "TIMEOUT"), row("c", 500)],
      },
    ]);
    expect(data[0].runs[0].sequence).toEqual([100, null, 500]);
  });

  it("averages each percentile across runs", () => {
    const data = buildReportData([
      { name: "conductor-100-2026-05-29T10-00-00-000Z.csv", rows: [row("a", 100), row("b", 100), row("c", 100)] },
      { name: "conductor-100-2026-05-29T11-00-00-000Z.csv", rows: [row("a", 300), row("b", 300), row("c", 300)] },
    ]);
    const c = data[0];
    expect(c.avg.p50).toBe(200);
  });
});
