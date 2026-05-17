---
layout: default
---

# Time to "hello workflow"

```
Cadence    │██████████████░░│  3–5 min   compose pulls Cassandra
Temporal   │██░░░░░░░░░░░░░░│  <60 s   single binary
Conductor  │███░░░░░░░░░░░░░│  <90 s   1 Docker image
Dapr       │██████░░░░░░░░░░│  90–120 s  dapr init pulls 3 containers
Restate    │██░░░░░░░░░░░░░░│  <30 s   single binary
```

Cassandra cold-pulls are why Cadence falls behind. Pin and pre-pull on stage.
