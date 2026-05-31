---
layout: default
footer: |
  <span>Marc Duiker</span>
  <img src=".demo/assets/diagrid-logo.webp" alt="Diagrid" style="height: 20px; width: auto;">
---

# Management API

| Operation | Cadence | Temporal | Conductor | Dapr | Restate |
|---|---|---|---|---|---|
| **Start** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Pause** | ❌ | ✅ *experimental* | ✅ | ✅ | ✅ |
| **Resume** | ❌ | ✅ *experimental* | ✅ | ✅ | ✅ |
| **Terminate** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Purge** | ❌ | ✅ | ✅ | ✅ | ✅ |

<!--
- **Cadence**: no instance pause/resume (open since 2017); purge only via retention, not on-demand.
- **Temporal**: native instance pause/resume is new in 2026 and still experimental.
- **Restate**: also auto-pauses on retry exhaustion.
- **Terminate**: Cadence, Temporal & Restate also expose a graceful *cancel*; Conductor & Dapr are terminate-only (cancel handled in code).
- **Access**: Conductor & Dapr offer plain REST; Temporal & Cadence are gRPC/CLI-first; Restate uses HTTP ingress + Admin API/CLI.
-->