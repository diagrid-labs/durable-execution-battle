import { describe, it, expect } from "vitest";
import { rowsToCsv, parseCsv } from "../csv.ts";
import type { WorkflowRow } from "../stats.ts";

describe("rowsToCsv", () => {
  it("writes header + one line per row in stable column order", () => {
    const rows: WorkflowRow[] = [
      { id: "ship-0001", t_start_ms: 1, t_end_ms: 100, latency_ms: 99, status: "COMPLETED" },
      { id: "ship-0002", t_start_ms: 2, t_end_ms: 50,  latency_ms: 48, status: "TIMEOUT" },
    ];
    const csv = rowsToCsv(rows);
    expect(csv).toBe(
      "id,t_start_ms,t_end_ms,latency_ms,status\n" +
      "ship-0001,1,100,99,COMPLETED\n" +
      "ship-0002,2,50,48,TIMEOUT\n"
    );
  });

  it("returns header-only string for empty input", () => {
    expect(rowsToCsv([])).toBe("id,t_start_ms,t_end_ms,latency_ms,status\n");
  });
});

describe("parseCsv", () => {
  it("parses a header + data rows into typed WorkflowRows", () => {
    const text =
      "id,t_start_ms,t_end_ms,latency_ms,status\n" +
      "a,1000,1100,100,COMPLETED\n" +
      "b,1000,1900,900,TIMEOUT\n";
    const rows = parseCsv(text);
    expect(rows).toEqual([
      { id: "a", t_start_ms: 1000, t_end_ms: 1100, latency_ms: 100, status: "COMPLETED" },
      { id: "b", t_start_ms: 1000, t_end_ms: 1900, latency_ms: 900, status: "TIMEOUT" },
    ]);
  });

  it("ignores a trailing blank line", () => {
    const text = "id,t_start_ms,t_end_ms,latency_ms,status\na,1,2,1,COMPLETED\n\n";
    expect(parseCsv(text)).toHaveLength(1);
  });

  it("round-trips with rowsToCsv", () => {
    const rows: WorkflowRow[] = [
      { id: "a", t_start_ms: 1000, t_end_ms: 1100, latency_ms: 100, status: "COMPLETED" },
      { id: "c", t_start_ms: 1000, t_end_ms: 1500, latency_ms: 500, status: "COMPLETED" },
    ];
    expect(parseCsv(rowsToCsv(rows))).toEqual(rows);
  });
});
