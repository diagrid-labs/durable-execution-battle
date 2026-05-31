import { existsSync, readFileSync, readdirSync, writeFileSync } from "node:fs";
import { fileURLToPath, pathToFileURL } from "node:url";
import { dirname, join } from "node:path";
import { parseCsv } from "./csv.ts";
import { percentile, type WorkflowRow } from "./stats.ts";

export interface RunData {
  label: string;
  source: string;
  /** latency_ms of COMPLETED rows only — used for percentiles and the ECDF. */
  latencies: number[];
  /** latency_ms per row in original workflow order; null for non-COMPLETED rows (drawn as a gap). */
  sequence: (number | null)[];
  completed: number;
  errors: number;
  p50: number;
  p95: number;
  p99: number;
}

export interface EngineReport {
  engine: string;
  runs: RunData[];
  avg: { p50: number; p95: number; p99: number };
}

export type ReportData = EngineReport[];

export interface CsvFile {
  name: string;
  rows: WorkflowRow[];
}

/** Engine name = first segment of a `<engine>-<concurrency>-<timestamp>.csv` filename. */
function engineOf(filename: string): string {
  return filename.split("-")[0];
}

/** Display order for engines, matching the tables in the slides/markdown files. */
const ENGINE_ORDER = ["cadence", "temporal", "conductor", "dapr", "restate"];

/** Rank for sorting; unknown engines sort after the known ones. */
function engineRank(engine: string): number {
  const i = ENGINE_ORDER.indexOf(engine);
  return i === -1 ? ENGINE_ORDER.length : i;
}

function avg(nums: number[]): number {
  if (nums.length === 0) return 0;
  return nums.reduce((a, b) => a + b, 0) / nums.length;
}

export function buildReportData(files: CsvFile[]): ReportData {
  const byEngine = new Map<string, CsvFile[]>();
  for (const f of files) {
    const engine = engineOf(f.name);
    const list = byEngine.get(engine);
    if (list) list.push(f);
    else byEngine.set(engine, [f]);
  }

  const engines = [...byEngine.keys()].sort(
    (a, b) => engineRank(a) - engineRank(b) || a.localeCompare(b),
  );
  return engines.map((engine) => {
    const sorted = byEngine.get(engine)!.slice().sort((a, b) => a.name.localeCompare(b.name));
    const runs: RunData[] = sorted.map((f, i) => {
      const completedRows = f.rows.filter((r) => r.status === "COMPLETED");
      const latencies = completedRows.map((r) => r.latency_ms);
      const sequence = f.rows.map((r) => (r.status === "COMPLETED" ? r.latency_ms : null));
      return {
        label: `Run ${i + 1}`,
        source: f.name,
        latencies,
        sequence,
        completed: completedRows.length,
        errors: f.rows.length - completedRows.length,
        p50: latencies.length ? percentile(latencies, 50) : 0,
        p95: latencies.length ? percentile(latencies, 95) : 0,
        p99: latencies.length ? percentile(latencies, 99) : 0,
      };
    });
    return {
      engine,
      runs,
      avg: {
        p50: avg(runs.map((r) => r.p50)),
        p95: avg(runs.map((r) => r.p95)),
        p99: avg(runs.map((r) => r.p99)),
      },
    };
  });
}

function renderHtml(data: ReportData, plotlyJs: string): string {
  const chartSections = data
    .map(
      (e) => `
  <section class="engine">
    <h2>${e.engine}</h2>
    <div class="charts">
      <div id="ecdf-${e.engine}" class="chart"></div>
      <div id="seq-${e.engine}" class="chart"></div>
    </div>
  </section>`,
    )
    .join("\n");

  const tables = data
    .map(
      (e) => `
  <section class="engine-table">
    <h3>${e.engine}</h3>
    <table>
      <thead><tr><th>Run</th><th>p50 (ms)</th><th>p95 (ms)</th><th>p99 (ms)</th><th>completed</th><th>errors</th></tr></thead>
      <tbody>
        ${e.runs
          .map(
            (r) =>
              `<tr><td>${r.label}</td><td>${r.p50.toFixed(0)}</td><td>${r.p95.toFixed(0)}</td><td>${r.p99.toFixed(0)}</td><td>${r.completed}</td><td>${r.errors}</td></tr>`,
          )
          .join("\n        ")}
        <tr class="avg"><td>Average</td><td>${e.avg.p50.toFixed(0)}</td><td>${e.avg.p95.toFixed(0)}</td><td>${e.avg.p99.toFixed(0)}</td><td></td><td></td></tr>
      </tbody>
    </table>
  </section>`,
    )
    .join("\n");

  return `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8" />
<title>Durable Execution Battle — performance</title>
<style>
  body { font-family: system-ui, sans-serif; margin: 2rem; color: #1a1a1a; }
  h1 { margin-bottom: 0.25rem; }
  .note { color: #555; max-width: 60rem; font-size: 0.9rem; }
  .explain { max-width: 60rem; background: #f4f7fb; border: 1px solid #d6e0ec;
             border-radius: 6px; padding: 1rem 1.25rem; margin: 1.5rem 0; font-size: 0.9rem; }
  .explain h2 { margin-top: 0; font-size: 1.05rem; }
  .explain ul { margin: 0.5rem 0; padding-left: 1.2rem; }
  .explain li { margin: 0.25rem 0; }
  .engine { margin: 3rem 0; }
  table { border-collapse: collapse; margin: 1rem 0; }
  th, td { border: 1px solid #ccc; padding: 0.3rem 0.7rem; text-align: right; }
  th:first-child, td:first-child { text-align: left; }
  tr.avg { font-weight: 600; background: #f4f4f4; }
  .charts { display: flex; flex-direction: column; gap: 1.5rem; }
  .chart { width: 100%; height: 420px; }
</style>
<script>${plotlyJs}</script>
</head>
<body>
<h1>Durable Execution Battle — performance</h1>
<p class="note">
  Conference-talk comparison, not a production benchmark. 200&nbsp;ms status-poll
  interval is a duration floor &mdash; p50 below ~250&nbsp;ms is meaningless. All
  engines run as single-node local stacks on default config; numbers reflect
  orchestration overhead, not application logic.
</p>
<section class="explain">
  <h2>How to read the duration ECDF charts</h2>
  <p>
    <strong>ECDF</strong> stands for <strong>Empirical Cumulative Distribution Function</strong>.
    It is built straight from the measured runs (<em>empirical</em>) and shows, for any
    duration on the x-axis, the percentage of workflows that completed at or below it
    (<em>cumulative</em>).
  </p>
  <ul>
    <li><strong>X-axis:</strong> workflow duration (ms). <strong>Y-axis:</strong> cumulative % of workflows (0&ndash;100%).</li>
    <li><strong>To read a percentile:</strong> pick a % on the Y-axis, slide right to the curve, then drop to the X-axis &mdash; that is the duration that many workflows beat. Y&nbsp;=&nbsp;50% is the median (p50), 95% is p95, 99% is p99. The dashed vertical lines mark the averaged p50/p95/p99.</li>
    <li><strong>Shape:</strong> a steep curve hugging the left means fast and consistent; a long tail stretching right means a few workflows were much slower than the rest (the slow tail).</li>
    <li><strong>Three lines:</strong> one per benchmark run. Lines sitting on top of each other mean the engine is reproducible; a line shifted right was a slower run.</li>
    <li><strong>Caveat:</strong> the 200&nbsp;ms status-poll interval is a duration floor, so anything left of ~250&nbsp;ms is a polling artifact &mdash; compare engines by the right-hand tail.</li>
  </ul>
</section>
${chartSections}
<section class="tables">
  <h2>Summary tables (p50 / p95 / p99)</h2>
  ${tables}
</section>
<script>
  const REPORT = ${JSON.stringify(data)};
  const COLORS = ["#1f77b4", "#ff7f0e", "#2ca02c", "#d62728", "#9467bd"];

  function ecdf(latencies) {
    const xs = [...latencies].sort((a, b) => a - b);
    const ys = xs.map((_, i) => ((i + 1) / xs.length) * 100);
    return { x: xs, y: ys };
  }

  for (const e of REPORT) {
    const ecdfTraces = e.runs.map((r, i) => {
      const c = ecdf(r.latencies);
      return { x: c.x, y: c.y, mode: "lines", name: r.label, line: { color: COLORS[i % COLORS.length] } };
    });
    const pctLines = [
      { p: e.avg.p50, label: "avg p50" },
      { p: e.avg.p95, label: "avg p95" },
      { p: e.avg.p99, label: "avg p99" },
    ];
    Plotly.newPlot("ecdf-" + e.engine, ecdfTraces, {
      title: { text: e.engine + " — duration ECDF" },
      xaxis: { title: { text: "duration (ms)" }, automargin: true },
      yaxis: { title: { text: "cumulative %" }, range: [0, 100], automargin: true },
      shapes: pctLines.map((pl) => ({
        type: "line", x0: pl.p, x1: pl.p, y0: 0, y1: 100,
        line: { color: "#888", width: 1, dash: "dash" },
      })),
      annotations: pctLines.map((pl) => ({
        x: pl.p, y: 100, text: pl.label, showarrow: false, font: { size: 10 }, yanchor: "bottom",
      })),
      margin: { t: 40 },
    }, { responsive: true });

    const seqTraces = e.runs.map((r, i) => ({
      x: r.sequence.map((_, idx) => idx + 1),
      y: r.sequence,
      mode: "lines",
      name: r.label,
      connectgaps: false,
      line: { color: COLORS[i % COLORS.length] },
    }));
    Plotly.newPlot("seq-" + e.engine, seqTraces, {
      title: { text: e.engine + " — duration per workflow" },
      xaxis: { title: { text: "workflow #" }, automargin: true },
      yaxis: { title: { text: "duration (ms)" }, automargin: true },
      shapes: pctLines.map((pl) => ({
        type: "line", xref: "paper", x0: 0, x1: 1, y0: pl.p, y1: pl.p,
        line: { color: "#888", width: 1, dash: "dash" },
      })),
      annotations: pctLines.map((pl) => ({
        xref: "paper", x: 1, y: pl.p, text: pl.label, showarrow: false, font: { size: 10 }, xanchor: "right",
      })),
      margin: { t: 40 },
    }, { responsive: true });
  }
</script>
</body>
</html>`;
}

function main(): void {
  const here = dirname(fileURLToPath(import.meta.url));
  const resultsDir = join(here, "..", "results");
  if (!existsSync(resultsDir)) {
    console.error("Results directory not found: " + resultsDir + "\nRun a benchmark first (see README).");
    process.exit(1);
  }

  const files: CsvFile[] = readdirSync(resultsDir)
    .filter((n) => n.endsWith(".csv"))
    .map((name) => ({ name, rows: parseCsv(readFileSync(join(resultsDir, name), "utf8")) }));

  if (files.length === 0) {
    console.error("No CSV files found in " + resultsDir);
    process.exit(1);
  }

  const plotlyPath = join(here, "..", "node_modules", "plotly.js-dist-min", "plotly.min.js");
  if (!existsSync(plotlyPath)) {
    console.error("Plotly bundle not found at " + plotlyPath + "\nRun `npm install` first.");
    process.exit(1);
  }

  const data = buildReportData(files);
  const plotlyJs = readFileSync(plotlyPath, "utf8");
  const html = renderHtml(data, plotlyJs);
  const out = join(here, "..", "report.html");
  writeFileSync(out, html);
  console.log("Wrote " + out + " (" + files.length + " CSV files, " + data.length + " engines)");
}

if (import.meta.url === pathToFileURL(process.argv[1]).href) {
  main();
}
