import type { Summary } from "./stats.ts";

export const ENGINES = ["cadence", "temporal", "conductor", "dapr", "restate"] as const;
export type EngineName = (typeof ENGINES)[number];

const HEADER = [
  "# Performance — local burst (testing=true, wait skipped)",
  "",
  "| Engine    | N   | Wallclock | Throughput | p50    | p95    | p99    | Errors | Run at           |",
  "|-----------|-----|-----------|------------|--------|--------|--------|--------|------------------|",
];

const FOOTER = [
  "",
  "_Methodology: single-node local engines, default config; bursts of N workflows with `testing=true` (60s wait skipped); 200 ms status-poll floor; 5-workflow warmup discarded._",
  "",
];

function pendingRow(engine: EngineName): string {
  // Column widths chosen to match the header so the table stays aligned in source.
  const name = engine.padEnd(9, " ");
  return `| ${name} | —   | _pending_ |            |        |        |        |        |                  |`;
}

function filledRow(engine: EngineName, s: Summary, runAt: string): string {
  const name = engine.padEnd(9, " ");
  const n = String(s.n).padEnd(3, " ");
  const wall = `${(s.wallclock_ms / 1000).toFixed(1)} s`.padEnd(9, " ");
  const tput = `${s.throughput_wps.toFixed(1)} wf/s`.padEnd(10, " ");
  const p50 = `${Math.round(s.p50_ms)} ms`.padEnd(6, " ");
  const p95 = `${Math.round(s.p95_ms)} ms`.padEnd(6, " ");
  const p99 = `${Math.round(s.p99_ms)} ms`.padEnd(6, " ");
  const errs = String(s.errors).padEnd(6, " ");
  const at = runAt.padEnd(16, " ");
  return `| ${name} | ${n} | ${wall} | ${tput} | ${p50} | ${p95} | ${p99} | ${errs} | ${at} |`;
}

export function initialResultsMarkdown(): string {
  return [
    ...HEADER,
    ...ENGINES.map(pendingRow),
    ...FOOTER,
  ].join("\n");
}

function replaceEngineRow(md: string, engine: EngineName, newRow: string): string {
  const lines = md.split("\n");
  // Match a line starting with "| <engine>" possibly followed by spaces and a "|".
  const rowRegex = new RegExp(`^\\|\\s*${engine}\\b.*\\|\\s*$`);
  const idx = lines.findIndex((l) => rowRegex.test(l));
  if (idx === -1) {
    throw new Error(`results-md: no row found for engine "${engine}"`);
  }
  lines[idx] = newRow;
  return lines.join("\n");
}

export function mergeRow(md: string, engine: EngineName, s: Summary, runAt: string): string {
  return replaceEngineRow(md, engine, filledRow(engine, s, runAt));
}

export function resetRow(md: string, engine: EngineName): string {
  return replaceEngineRow(md, engine, pendingRow(engine));
}
