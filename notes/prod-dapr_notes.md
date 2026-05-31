# Notes for Production — Dapr Workflows

The diagram shows the typical Kubernetes deployment: a Dapr control plane plus worker pods that each embed the workflow engine in their `daprd` sidecar. **There is no dedicated workflow cluster** — the engine scales with your app pods.

Nodes in the diagram:

- **Dapr Control Plane (K8s)** — the subgraph of cluster-wide services:
  - **Operator** — manages Dapr component/configuration CRDs and the runtime on Kubernetes.
  - **Sidecar Injector** — injects the `daprd` sidecar into each annotated app pod (`Injector --> A1/A2/A3`).
  - **Sentry (mTLS)** — the certificate authority that issues/rotates workload certificates for mTLS (`Sentry -.mTLS.-> A1`).
  - **Placement service** — maintains the actor placement table; workflow/activity actors are looked up here for load-balancing across nodes (`actor lookup`).
  - **Scheduler (HA + etcd)** — stores and fires workflow timers and reminders, backed by its own embedded etcd.
- **Workflow Worker Pods** — the data-plane subgraph of **App + daprd engine** pods (A1/A2/A3). Each pod runs your app code plus a `daprd` sidecar containing the embedded `durabletask-go` engine and the actor runtime.
- **External state store** — transactional + ETag store where workflow history/state is event-sourced.

The **key constraint**: a single workflow instance is a single-threaded actor pinned to one node, so an individual workflow can't be scaled (activities can still fan out across pods). HA is delegated to the chosen state store + the Scheduler's etcd; there is no built-in cross-region workflow replication — multi-region rides on the datastore's geo-replication.
