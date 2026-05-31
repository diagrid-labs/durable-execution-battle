---
layout: default
footer: |
  <span>Marc Duiker</span>
  <img src=".demo/assets/diagrid-logo.webp" alt="Diagrid" style="height: 20px; width: auto;">
---

# Workflow features

| Feature | Cadence | Temporal | Conductor | Dapr | Restate |
|---|---|---|---|---|---|
| **Cross-language orchestration** | — *(Go + Java only)* | Nexus (v1.31 GA) | native — workers in any language | HTTP API — start/signal from anywhere | native — handlers are HTTP |
| **Versioning** *(not a pattern, but worth comparing)* | `workflow.GetVersion` | Worker Deployment Versioning (GA) + patching | integer DSL version per workflow | `IsPatched` + named workflow versions *(since 1.17)* | service revisions on redeploy |
