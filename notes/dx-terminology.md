**Footnotes:**
- ¹ Restate has no tenancy primitive in the OSS server — *Environment* is a **Restate Cloud** concept only
- ² Dapr and Restate have **no pollable task queue** — they push work instead: Dapr hash-distributes workflow/activity *actors* across an app-id's sidecars via the Placement service; Restate hashes keys to *partitions* and pushes each invocation to your registered HTTP deployment
- ³ Temporal/Cadence emulate a stateful object with the long-running **"Entity Workflow"** pattern — a convention, not a primitive (Conductor has no equivalent)