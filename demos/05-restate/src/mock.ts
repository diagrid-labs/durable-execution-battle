// src/mock.ts
//
// Non-determinism lives here, not in the workflow body. The workflow's `run`
// handler is replayed by Restate from the journal; Math.random() inside it
// would break replay. These functions are called only from `services.ts`
// handlers, which are journaled call-by-call — Restate stores the *return
// value*, so random results are stable across replay.

import type { ScanResult, Priority, Severity, SubsystemId } from "./models.ts";

export function generateScan(
  subsystem: SubsystemId,
  candidates: string[],
): ScanResult {
  // Biased low: health ∈ [30, 65] so min(health) < 50 is overwhelmingly likely.
  const health = 30 + Math.floor(Math.random() * 36);

  // 1..3 anomalies, sampled without replacement from the candidate list.
  const count = 1 + Math.floor(Math.random() * 3);
  const shuffled = [...candidates].sort(() => Math.random() - 0.5);

  return {
    subsystem,
    health,
    anomalies: shuffled.slice(0, count),
  };
}

export function prioritize(scans: ScanResult[]): Priority {
  if (scans.length === 0) {
    return { severity: "green", summary: "no scans submitted" };
  }

  // noUncheckedIndexedAccess: scans[0] is ScanResult | undefined, but we
  // just guarded length > 0. The `!` is the explicit narrowing for the
  // type-checker; the runtime guard above is the real safety net.
  let worst: ScanResult = scans[0]!;
  for (const s of scans) {
    if (s.health < worst.health) worst = s;
  }

  const severity: Severity =
    worst.health < 50 ? "red_alert" :
    worst.health < 75 ? "yellow_alert" :
                        "green";

  const belowThreshold = scans.filter((s) => s.health < 75).length;
  const summary =
    `${worst.subsystem} critical at ${worst.health}% health; ` +
    `${belowThreshold} subsystem(s) below threshold`;

  return { severity, summary };
}
