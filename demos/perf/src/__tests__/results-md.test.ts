import { describe, it, expect } from "vitest";
import { mergeRow, resetRow, ENGINES, initialResultsMarkdown } from "../results-md.ts";
import type { Summary } from "../stats.ts";

const STARTER = initialResultsMarkdown();

describe("initialResultsMarkdown", () => {
  it("contains a pending row for every engine", () => {
    for (const e of ENGINES) {
      expect(STARTER).toContain(`| ${e} `);
      expect(STARTER).toContain("_pending_");
    }
  });
});

describe("mergeRow", () => {
  const summary: Summary = {
    n: 100, completed: 100, errors: 0,
    wallclock_ms: 4200, throughput_wps: 23.8,
    p50_ms: 180, p95_ms: 410, p99_ms: 520,
  };

  it("replaces only the targeted engine's row, leaves others untouched", () => {
    const updated = mergeRow(STARTER, "cadence", summary, "2026-05-28 14:02");
    expect(updated).toContain("| cadence   | 100 | 4.2 s     | 23.8 wf/s  | 180 ms | 410 ms | 520 ms | 0      | 2026-05-28 14:02 |");
    // other engines still pending
    expect(updated).toContain("| temporal  | —   | _pending_");
    expect(updated).toContain("| restate   | —   | _pending_");
  });

  it("running twice for the same engine overwrites the previous row", () => {
    const once = mergeRow(STARTER, "dapr", summary, "2026-05-28 14:02");
    const twiceSummary: Summary = { ...summary, throughput_wps: 99.9, p50_ms: 50 };
    const twice = mergeRow(once, "dapr", twiceSummary, "2026-05-28 15:00");
    expect(twice).toContain("99.9 wf/s");
    expect(twice).toContain("50 ms");
    expect(twice).toContain("2026-05-28 15:00");
    expect(twice).not.toContain("23.8 wf/s");
  });
});

describe("resetRow", () => {
  it("sets the targeted engine's row back to pending without touching others", () => {
    const summary: Summary = {
      n: 100, completed: 100, errors: 0,
      wallclock_ms: 4200, throughput_wps: 23.8,
      p50_ms: 180, p95_ms: 410, p99_ms: 520,
    };
    const filled = mergeRow(STARTER, "conductor", summary, "2026-05-28 14:02");
    const reset = resetRow(filled, "conductor");
    expect(reset).toContain("| conductor | —   | _pending_");
    // Other rows from STARTER remain pending; reset didn't affect them
    expect(reset).toContain("| cadence   | —   | _pending_");
  });
});
