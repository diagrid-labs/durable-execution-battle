## Plan — split `slides/*.md` into one slide per file

### Goal

Each existing multi-slide file under `slides/` becomes a subfolder of the same base name, containing one markdown file per slide. This makes it easier to insert new slides or polls without renumbering downstream `slideIndex` references in `.demo/` files.

### Naming convention

- Subfolder name = original file name without `.md` (e.g. `00-open.md` → `00-open/`).
- Slide file name = `NN-<slug>.md`, where `NN` is the zero-padded original slide index (matching today's `slideIndex` numbering) and `<slug>` is a short kebab-case slug derived from the slide title.
- Each new file keeps its existing frontmatter block (`---\nlayout: X\n---`) and its body verbatim — no edits to slide content.

### Full slide-to-file mapping

#### `slides/00-open.md` → `slides/00-open/`
| Index | Title | New file |
|---|---|---|
| 0 | Durable Execution Battle (title) | `00-title.md` |
| 1 | Disclosure | `01-disclosure.md` |
| 2 | What is durable execution? | `02-what-is-durable-execution.md` |
| 3 | The 5 contenders | `03-five-contenders.md` |
| 4 | Opening poll | `04-opening-poll.md` |
| 5 | Lineage map | `05-lineage-map.md` |
| 6 | 5 dimensions we'll evaluate | `06-dimensions-preview.md` |

#### `slides/01-dx.md` → `slides/01-dx/`
| Index | Title | New file |
|---|---|---|
| 0 | Dimension 1 — Developer Experience | `00-section.md` |
| 1 | DX matrix | `01-dx-matrix.md` |
| 2 | Time to "hello workflow" | `02-hello-workflow.md` |
| 3 | Web UIs | `03-web-uis.md` |
| 4 | Learning curve — two cliffs | `04-learning-curve.md` |

#### `slides/02-languages.md` → `slides/02-languages/`
| Index | Title | New file |
|---|---|---|
| 0 | Dimension 2 — Language Support & Features | `00-section.md` |
| 1 | SDK matrix | `01-sdk-matrix.md` |
| 2 | Feature comparison | `02-feature-comparison.md` |
| 3 | Cadence Python — archived | `03-cadence-python-archived.md` |
| 4 | Sagas + cron | `04-sagas-and-cron.md` |

#### `slides/03-hosting.md` → `slides/03-hosting/`
| Index | Title | New file |
|---|---|---|
| 0 | Dimension 3 — Hosting | `00-section.md` |
| 1 | Hosting matrix | `01-hosting-matrix.md` |
| 2 | Operational footprint | `02-operational-footprint.md` |
| 3 | Managed cloud landscape | `03-managed-cloud-landscape.md` |

#### `slides/04-performance.md` → `slides/04-performance/`
| Index | Title | New file |
|---|---|---|
| 0 | Dimension 4 — Performance & Scalability | `00-section.md` |
| 1 | Proven scale | `01-proven-scale.md` |
| 2 | Architectures at a glance | `02-architectures.md` |
| 3 | Bottlenecks at scale | `03-bottlenecks.md` |

#### `slides/05-demo-intro.md` → `slides/05-demo-intro/`
| Index | Title | New file |
|---|---|---|
| 0 | Demo block — 25 minutes | `00-demo-block.md` |
| 1 | The scenario | `01-scenario.md` |
| 2 | The kill-and-resume promise | `02-kill-and-resume.md` |

#### `slides/06-community.md` → `slides/06-community/`
| Index | Title | New file |
|---|---|---|
| 0 | Dimension 5 — Community & Support | `00-section.md` |
| 1 | Community matrix | `01-community-matrix.md` |
| 2 | Single-vendor risk | `02-single-vendor-risk.md` |

#### `slides/07-decision-guide.md` → `slides/07-decision-guide/`
| Index | Title | New file |
|---|---|---|
| 0 | Decision guide — "If you want X, pick Y" | `00-decision-guide.md` |

#### `slides/08-wrap.md` → `slides/08-wrap/`
| Index | Title | New file |
|---|---|---|
| 0 | Code + slides | `00-code-and-slides.md` |
| 1 | Thank you | `01-thank-you.md` |

### DemoTime reference updates

After the split, replace every `openSlide` call that uses `path` + `slideIndex` with a direct `path` to the per-slide file. Drop `slideIndex`.

| File | Current | New |
|---|---|---|
| `.demo/intro.yaml` | `path: slides/00-open.md` | `path: slides/00-open/00-title.md` |
| `.demo/00-opening-poll.demo` | `slides/00-open.md` + `slideIndex: 3` | `slides/00-open/03-five-contenders.md` |
| `.demo/00-opening-poll.demo` | `slides/00-open.md` + `slideIndex: 5` | `slides/00-open/05-lineage-map.md` |
| `.demo/04-dapr.demo` | `slides/05-demo-intro.md` + `slideIndex: 0` | `slides/05-demo-intro/00-demo-block.md` |
| `.demo/99-closing-poll.demo` | `slides/07-decision-guide.md` + `slideIndex: 0` | `slides/07-decision-guide/00-decision-guide.md` |

### Execution steps

1. **Create the 9 subfolders** under `slides/` matching the existing base filenames.
2. **For each original file**, split on the `---`-delimited frontmatter boundaries and write each slide block (frontmatter + body) verbatim into its mapped per-slide file inside the subfolder.
3. **Verify content parity**: concatenating each subfolder's files in numeric order should reproduce the original file byte-for-byte (minus any trailing-newline normalization). A quick `diff` between the original and `cat NN-*/*.md` is the integrity check.
4. **Delete the 9 original `slides/*.md` files** once parity is verified.
5. **Update the 5 DemoTime references** per the table above.
6. **Smoke-test in DemoTime**: open `intro.yaml`, run the `00-opening-poll` and `99-closing-poll` demo scripts, and confirm the right slides open.

### Out of scope

- No edits to slide *content*.
- No changes to `plan.md`, `research.md`, `README.md`, `.engage/*.json`, or any demo source under `demos/`.
- No introduction of a manifest/index file per folder; ordering is carried entirely by the `NN-` numeric prefix.

### Risks

- **Slug bikeshed**: the slugs above are derived from the current titles. If a title changes later, the filename either gets renamed (and DemoTime refs updated again) or drifts from the title. Numeric prefix protects ordering regardless.
- **DemoTime path resolution**: this plan assumes DemoTime accepts a per-slide file as an `openSlide` path with no `slideIndex`. The smoke test in step 6 is the gating check; if it fails, fall back to keeping a single aggregated file per section and revisit.
