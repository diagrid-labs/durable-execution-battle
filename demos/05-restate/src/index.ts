// src/index.ts
//
// Single Node process:
//   1. Restate SDK endpoint (HTTP/2) bound on :9080 — Restate pushes
//      invocations here.
//   2. Express wrapper (HTTP/1.1) on :3000 — audience POSTs /start.
//   3. Auto-registers the deployment with restate-server's admin API
//      (default :9070) so no manual `restate deployments register` step.
//
// One `npm run dev`; one `kill -9` brings both down — that's the durability
// demo's setup.

import * as restate from "@restatedev/restate-sdk";
import { diagnosticsWorkflow } from "./diagnostics.ts";
import { activities } from "./services.ts";
import { buildApi } from "./api.ts";

const HANDLER_PORT = Number(process.env.HANDLER_PORT ?? 9080);
const ADMIN_URL = process.env.RESTATE_ADMIN ?? "http://localhost:9070";

// 1. Restate handler endpoint.
restate
  .endpoint()
  .bind(diagnosticsWorkflow)
  .bind(activities)
  .listen(HANDLER_PORT);

// 2. Express wrapper.
buildApi();

// 3. Auto-register the deployment with Restate. force=true lets the
//    registrar replace the existing deployment at this URI without manual
//    cleanup across edit-rebuild cycles.
void (async () => {
  const uri = `http://localhost:${HANDLER_PORT}`;
  const maxAttempts = 5;
  const backoffMs = 2000;

  for (let attempt = 1; attempt <= maxAttempts; attempt++) {
    try {
      const res = await fetch(`${ADMIN_URL}/deployments`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ uri, force: true }),
      });
      if (res.ok || res.status === 409) {
        console.log(`Deployment registered at ${uri}`);
        return;
      }
      console.error(
        `Deployment registration attempt ${attempt} failed: ${res.status} ${await res.text()}`,
      );
    } catch (err) {
      console.error(`Deployment registration attempt ${attempt} threw: ${err}`);
    }
    if (attempt < maxAttempts) {
      await new Promise((r) => setTimeout(r, backoffMs));
    }
  }

  console.error(
    `Deployment registration failed after ${maxAttempts} attempts. ` +
      `Is restate-server running at ${ADMIN_URL}?`,
  );
  process.exit(1);
})();
