import { describe, it, expect } from "vitest";
import { percentile, summarize } from "../stats.ts";

describe("percentile", () => {
  it("returns the p50 of an odd-length sorted-or-unsorted array", () => {
    expect(percentile([5, 1, 3, 2, 4], 50)).toBe(3);
  });

  it("interpolates between values for even-length arrays", () => {
    // [1,2,3,4] p50 → between 2 and 3 → 2.5
    expect(percentile([1, 2, 3, 4], 50)).toBe(2.5);
  });

  it("returns the maximum for p100", () => {
    expect(percentile([10, 20, 30], 100)).toBe(30);
  });

  it("returns the minimum for p0", () => {
    expect(percentile([10, 20, 30], 0)).toBe(10);
  });

  it("throws on empty input", () => {
    expect(() => percentile([], 50)).toThrow();
  });
});

describe("summarize", () => {
  it("computes p50/p95/p99, throughput, wallclock, errors from raw rows", () => {
    const rows = [
      { id: "a", t_start_ms: 1000, t_end_ms: 1100, latency_ms: 100, status: "COMPLETED" as const },
      { id: "b", t_start_ms: 1000, t_end_ms: 1300, latency_ms: 300, status: "COMPLETED" as const },
      { id: "c", t_start_ms: 1000, t_end_ms: 1500, latency_ms: 500, status: "COMPLETED" as const },
      { id: "d", t_start_ms: 1000, t_end_ms: 1900, latency_ms: 900, status: "TIMEOUT" as const },
    ];
    const s = summarize(rows, /*wallclock_ms*/ 900);
    expect(s.n).toBe(4);
    expect(s.completed).toBe(3);
    expect(s.errors).toBe(1);
    expect(s.wallclock_ms).toBe(900);
    expect(s.throughput_wps).toBeCloseTo(3 / 0.9, 3);
    // Percentiles are over completed rows only
    expect(s.p50_ms).toBe(300);
    expect(s.p95_ms).toBeGreaterThanOrEqual(500);
    expect(s.p99_ms).toBeGreaterThanOrEqual(500);
  });
});
