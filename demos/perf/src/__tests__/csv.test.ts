import { describe, it, expect } from "vitest";
import { rowsToCsv } from "../csv.ts";
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
