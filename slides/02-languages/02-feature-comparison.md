---
layout: default
---

# Feature comparison

| | Cadence | Temporal | Conductor | Dapr | Restate |
|---|---|---|---|---|---|
| Signals / events | ✅ | ✅ | ✅ (event handlers) | ✅ | ✅ (awakeables) |
| Queries | ✅ | ✅ | (via state) | ✅ | ✅ |
| Child workflows | ✅ | ✅ | ✅ (sub-workflow) | ✅ | ✅ |
| Versioning | ✅ patch | ✅ Worker Deployment Versioning (GA) | (DSL versions) | ✅ since 1.17 | ✅ via service revisions |
| Scheduling / cron | ✅ | ✅ Schedules | ✅ | ✅ | (pattern) |
| Search attributes | ✅ ES-backed | ✅ ES-backed | ✅ | ❌ | (state queries) |
