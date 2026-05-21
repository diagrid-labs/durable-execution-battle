---
layout: default
footer: |
  <span>Marc Duiker</span>
  <img src=".demo/assets/diagrid-logo.webp" alt="Diagrid" style="height: 20px; width: auto;">
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
