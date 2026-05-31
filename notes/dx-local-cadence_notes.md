# Notes for Local dev — Cadence

  - Your worker process long-polls for tasks (the worker -->|long-poll tasks| fe edge). That long-poll request enters via the Frontend gateway and
  is served by Matching, which holds the pending workflow/activity tasks and dispatches them to whichever worker is polling the matching task list.
  - The History service (which advances workflow state) pushes new tasks into Matching (hist --> match), and Matching makes them available to
  pollers.

  So the division of labor is:
  - Frontend — stateless gateway / API surface (CLI, UI, and workers all talk to it)
  - History — advances and persists workflow execution state 
  - Matching — the task-dispatch layer between History's generated work and the workers that long-poll to execute it
  - Internal Worker — runs Cadence's own system workflows