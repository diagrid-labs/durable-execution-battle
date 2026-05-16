---
layout: default
---

# The kill-and-resume promise

1. Start workflow → 5 scans complete in ~5 s.
2. Workflow enters **60-second sleep**. *DemoTime overlays a countdown.*
3. **I kill the worker** mid-sleep.
4. I restart it.
5. Sleep durably wakes → prioritize → 🚨 bridge alert.

> *Same workflow, 5 frameworks, same kill point. Watch what survives.*
