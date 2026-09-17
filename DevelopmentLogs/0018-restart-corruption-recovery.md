# 0018 — Restart and corrupt-index recovery acceptance

Date: 2026-09-18
Branch: `linux-port`

## Goal

Advance Stage 8 release acceptance by proving that the Linux daemon survives a normal restart and recovers from a damaged persistent index without requiring manual cleanup.

## Decision

Recovery is tested at the process boundary rather than only through unit-level serialization tests. The acceptance scenario uses the same daemon and CLI entry points that an installed user session uses, while retaining the existing Release build produced by the `linux-core` job.

A deliberately invalid index payload is written only after a clean daemon shutdown. This avoids conflating corruption recovery with concurrent-writer behavior and makes the gate deterministic.

## Implementation

Updated `.github/workflows/linux-ci.yml` with an acceptance test that:

- starts the daemon against a temporary root and index;
- verifies a known file is searchable;
- shuts the daemon down cleanly and confirms a persistent index exists;
- restarts against that index and verifies the same result;
- replaces the persisted index with an invalid payload;
- starts the daemon again and requires it to rebuild/recover sufficiently for the known file to be searchable;
- shuts down cleanly and verifies a non-empty index remains.

The helper functions intentionally exercise `daemon-status`, `daemon-search`, and `daemon-shutdown` over the Unix-domain socket so readiness and shutdown semantics are part of the acceptance path.

## Validation

The change triggers `Linux Port CI`. This milestone is accepted only when the updated `linux-core` job and the existing publish/package matrix are green. If the corruption scenario exposes a daemon startup failure, the next development step must fix recovery behavior rather than weaken this gate.

## Next

Add representative large-index performance and memory acceptance thresholds. Then complete release documentation, clean-machine/GUI acceptance guidance, and synchronize the roadmap before final release sign-off.
