# Workflow management from the CLI

After a run — *can I list recent executions without copy-pasting an instance ID?*

| Engine    | CLI                                      | List recent workflows                                               |
|-----------|------------------------------------------|---------------------------------------------------------------------|
| Cadence   | ✅ `cadence` — bundled in the server image  | `docker exec cadence-server cadence --domain default workflow list` |
| Temporal  | ✅ `temporal`                            | `temporal workflow list`                                            |
| Conductor | ❌ none — REST / UI only                 | — (no CLI; use the UI, or REST API)                                 |
| Dapr      | ✅ `dapr`                                | `dapr workflow list -a wf-app`                                      |
| Restate   | ✅ `restate`                             | `restate invocations list`                                          |

_Three of five ship a first-class CLI. Cadence's CLI lives inside the server container; Conductor has none — the REST search endpoint (or the UI) is the way._
