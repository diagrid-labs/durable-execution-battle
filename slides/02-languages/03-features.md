---
layout: default
footer: |
  <span>Marc Duiker</span>
  <img src=".demo/assets/diagrid-logo.webp" alt="Diagrid" style="height: 20px; width: auto;">
---

# Workflow features

| Feature | Cadence | Temporal | Conductor | Dapr | Restate |
|---|---|---|---|---|---|
| **Multi-app workflows** *(deployment topology)* | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Versioning** | Patching via `workflow.GetVersion` | Worker Deployment Versioning + patching | Integer DSL version per workflow | named workflow versions + patching | service revisions on redeploy |
