import { writeFile, mkdir } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { initialResultsMarkdown } from "./results-md.ts";

const here = path.dirname(fileURLToPath(import.meta.url));
const out = path.resolve(here, "../../../slides/04-performance/04-results.md");
await mkdir(path.dirname(out), { recursive: true });
await writeFile(out, initialResultsMarkdown(), "utf8");
console.log("wrote", out);
