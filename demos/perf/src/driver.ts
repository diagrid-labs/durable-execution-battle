import { writeFile, readFile, mkdir } from "node:fs/promises";
import { existsSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

import { rowsToCsv } from "./csv.ts";
import { summarize, type WorkflowRow } from "./stats.ts";
import {
  ENGINES, type EngineName,
  initialResultsMarkdown, mergeRow, resetRow,
} from "./results-md.ts";
import type { EngineAdapter } from "./engines/types.ts";
import { createCadence } from "./engines/cadence.ts";
import { createTemporal } from "./engines/temporal.ts";
import { createConductor } from "./engines/conductor.ts";
import { createDapr } from "./engines/dapr.ts";
import { createRestate } from "./engines/restate.ts";

interface Args {
  engine: EngineName;
  baseUrl: string;
  n: number;
  concurrency: number;
  warmup: number;
  reset: boolean;
  pollMs: number;
  timeoutS: number;
}

const DEFAULT_BASE_URLS: Record<EngineName, string> = {
  cadence:   "http://localhost:8080",
  temporal:  "http://localhost:8080",
  conductor: "http://localhost:5050",
  dapr:      "http://localhost:5467",   // Aspire-assigned ApiService port (matches .http file); pass --base-url if Aspire reassigns
  restate:   "http://localhost:3000",   // Express API in front of Restate ingress (:8080)
};

function parseArgs(argv: string[]): Args {
  const get = (flag: string, dflt?: string): string | undefined => {
    const i = argv.indexOf(flag);
    if (i === -1) return dflt;
    return argv[i + 1];
  };
  const engine = get("--engine") as EngineName | undefined;
  if (!engine || !ENGINES.includes(engine)) {
    throw new Error(`--engine is required and must be one of: ${ENGINES.join(", ")}`);
  }
  return {
    engine,
    baseUrl: get("--base-url", DEFAULT_BASE_URLS[engine])!,
    n: Number(get("--n", "100")),
    concurrency: Number(get("--concurrency", get("--n", "100"))),
    warmup: Number(get("--warmup", "5")),
    reset: argv.includes("--reset"),
    pollMs: Number(get("--poll-ms", "200")),
    timeoutS: Number(get("--timeout-s", "120")),
  };
}

function createAdapter(engine: EngineName, baseUrl: string): EngineAdapter {
  switch (engine) {
    case "cadence":   return createCadence(baseUrl);
    case "temporal":  return createTemporal(baseUrl);
    case "conductor": return createConductor(baseUrl);
    case "dapr":      return createDapr(baseUrl);
    case "restate":   return createRestate(baseUrl);
  }
}

async function runWithConcurrencyLimit<T>(
  items: T[],
  limit: number,
  fn: (item: T, index: number) => Promise<void>,
): Promise<void> {
  let cursor = 0;
  const workers = Array.from({ length: Math.min(limit, items.length) }, async () => {
    while (true) {
      const i = cursor++;
      if (i >= items.length) return;
      await fn(items[i], i);
    }
  });
  await Promise.all(workers);
}

async function waitForCompletion(
  adapter: EngineAdapter,
  id: string,
  pollMs: number,
  timeoutS: number,
): Promise<"COMPLETED" | "TIMEOUT"> {
  const deadline = Date.now() + timeoutS * 1000;
  while (Date.now() < deadline) {
    try {
      if (await adapter.isComplete(id)) return "COMPLETED";
    } catch {
      // transient errors are absorbed; the poll loop will retry
    }
    await new Promise((r) => setTimeout(r, pollMs));
  }
  return "TIMEOUT";
}

function formatRunAt(d: Date): string {
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

async function ensureResultsMarkdown(resultsPath: string): Promise<string> {
  if (existsSync(resultsPath)) {
    return await readFile(resultsPath, "utf8");
  }
  const initial = initialResultsMarkdown();
  await mkdir(path.dirname(resultsPath), { recursive: true });
  await writeFile(resultsPath, initial, "utf8");
  return initial;
}

async function main() {
  const args = parseArgs(process.argv.slice(2));
  const adapter = createAdapter(args.engine, args.baseUrl);

  console.log(
    `[perf] engine=${args.engine} n=${args.n} warmup=${args.warmup} concurrency=${args.concurrency}` +
    ` baseUrl=${args.baseUrl}` + (args.reset ? " --reset" : ""),
  );

  // Repository-root-relative paths so the driver works regardless of cwd.
  const here = path.dirname(fileURLToPath(import.meta.url));
  const repoRoot = path.resolve(here, "../../..");          // demos/perf/src → repo root
  const resultsMdPath = path.join(repoRoot, "slides/04-performance/04-results.md");
  const resultsDir = path.join(repoRoot, "demos/perf/results");

  let md = await ensureResultsMarkdown(resultsMdPath);

  if (args.reset) {
    md = resetRow(md, args.engine);
    await writeFile(resultsMdPath, md, "utf8");
    console.log(`[perf] reset ${args.engine} row → pending`);
  }

  // --- Warmup ---
  if (args.warmup > 0) {
    console.log(`[perf] warmup: ${args.warmup} workflows`);
    const warmupIds = Array.from({ length: args.warmup }, (_, i) => `warmup-${Date.now()}-${i}`);
    await runWithConcurrencyLimit(warmupIds, args.warmup, async (id) => {
      const wfId = await adapter.start(adapter.buildInput(id));
      await waitForCompletion(adapter, wfId, args.pollMs, args.timeoutS);
    });
    console.log("[perf] warmup complete");
  }

  // --- Measured run ---
  const ids = Array.from({ length: args.n }, (_, i) => `perf-${Date.now()}-${String(i).padStart(5, "0")}`);
  const rows: WorkflowRow[] = new Array(args.n);

  const wallStart = Date.now();
  await runWithConcurrencyLimit(ids, args.concurrency, async (id, i) => {
    const tStart = Date.now();
    let status: WorkflowRow["status"] = "TIMEOUT";
    try {
      const wfId = await adapter.start(adapter.buildInput(id));
      status = await waitForCompletion(adapter, wfId, args.pollMs, args.timeoutS);
    } catch (e) {
      status = "TIMEOUT";
      console.error(`[perf] ${id} failed:`, (e as Error).message);
    }
    const tEnd = Date.now();
    rows[i] = {
      id,
      t_start_ms: tStart,
      t_end_ms: tEnd,
      latency_ms: tEnd - tStart,
      status,
    };
  });
  const wallEnd = Date.now();

  const summary = summarize(rows, wallEnd - wallStart);
  console.log(
    `[perf] done: n=${summary.n} completed=${summary.completed} errors=${summary.errors}` +
    ` wallclock=${(summary.wallclock_ms / 1000).toFixed(2)}s` +
    ` throughput=${summary.throughput_wps.toFixed(2)}wf/s` +
    ` p50=${Math.round(summary.p50_ms)}ms p95=${Math.round(summary.p95_ms)}ms p99=${Math.round(summary.p99_ms)}ms`,
  );

  // --- Write CSV ---
  await mkdir(resultsDir, { recursive: true });
  const csvName = `${args.engine}-${args.n}-${new Date().toISOString().replace(/[:.]/g, "-")}.csv`;
  const csvPath = path.join(resultsDir, csvName);
  await writeFile(csvPath, rowsToCsv(rows), "utf8");
  console.log(`[perf] csv → ${csvPath}`);

  // --- Merge into results.md ---
  const updated = mergeRow(md, args.engine, summary, formatRunAt(new Date()));
  await writeFile(resultsMdPath, updated, "utf8");
  console.log(`[perf] updated ${resultsMdPath}`);
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
