# 0026 — Formal release matrix gate

Date: 2026-09-18
Branch: `linux-port`

## Goal

Make the final automated release decision explicit rather than inferring release readiness from several independent green jobs.

## Decision

Keep the existing Linux CI jobs as the source of truth and add one aggregate `release-matrix` gate. It runs after `linux-core`, the complete x64/arm64 self-contained publish matrix, and the x64/arm64 Debian package matrix. The gate uses `always()` so a failed or cancelled prerequisite produces an explicit failed release decision rather than silently skipping the sign-off job.

No duplicate build/test work is introduced. This follows the repository's narrow-scope rule while still giving the formal Release Flow one auditable aggregate result.

## Implementation

Updated `.github/workflows/linux-ci.yml` with `Formal release matrix gate`. It records the candidate SHA and requires all three prerequisite job groups to report `success`.

The existing prerequisites already cover targeted MSTest, Release builds, graphical Xvfb startup, direct and persistent search, daemon IPC/watcher behavior, restart/corruption recovery, 50k-index performance/RSS limits, x64/arm64 self-contained publishing, Debian archive validation, real x64 package install/search/recovery/remove, and packaged systemd-unit validation.

## Validation

Changing the workflow triggers `Linux Port CI` on `linux-port`. This milestone is accepted only when the new aggregate gate itself is green. The release checklist must not be marked complete until that CI evidence exists.

## Remaining release blockers

- record a clean Ubuntu GNOME/Wayland manual smoke test;
- record KDE/Plasma validation where practical, or document the unavailable/manual limitation;
- after those desktop checks, run/identify the final green release candidate matrix and write the exact commit/run into the final acceptance log.
