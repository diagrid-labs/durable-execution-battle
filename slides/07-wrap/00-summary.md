---
layout: default
footer: |
  <span>Marc Duiker</span>
  <img src=".demo/assets/diagrid-logo.webp" alt="Diagrid" style="height: 20px; width: auto;">
---

# Summary

After the evaluations, two stand out as **mature, actively-developed, feature-complete, workflow-as-code** engines:

| | Why it's in front |
|---|---|
| **Temporal** | 7 SDKs · great Web UI · weekly releases · **MIT, no feature paywall** · Nexus cross-app workflows · multi-cloud HA failover (AWS↔GCP) · proven at scale (130B+ actions/mo on Cloud) |
| **Dapr Workflow** | DTFx engine lineage · **CNCF graduated** · 5 SDKs + versioning · cross-app workflows · **Apache 2.0, no feature paywall** · no separate workflow cluster (sidecar-embedded) · bring-your-own datastore |

**Why not the other three?**

- **Cadence** — the original, but the latest tagged release is **>12 months old**; Go + Java only, lacks in management operations.
- **Conductor** — workflow-as-**data**, not code.
- **Restate** — the youngest (2023), smallest community, source-available (BSL 1.1).
