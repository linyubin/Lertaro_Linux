# 0018 — Large-index performance and memory acceptance

Date: 2026-09-18
Branch: `linux-port`

## Goal

Add a repeatable Stage 8 performance gate that catches gross indexing/search regressions before release.

## Decision

Use the existing CLI and standard Ubuntu runner tooling rather than adding benchmark dependencies or production instrumentation. The acceptance fixture contains 50,000 empty `.pcd` files distributed across 100 directories. This is large enough to exercise filesystem enumeration, index serialization/loading, and fuzzy search while remaining practical on shared GitHub-hosted runners.

The gate intentionally uses generous regression ceilings rather than treating CI wall-clock time as a precise benchmark:

- initial index build must complete in under 30 seconds;
- indexed search must complete in under 3 seconds, including `dotnet run --no-build` process startup;
- peak RSS for both operations must remain below 512 MiB.

These are release safety rails, not product performance claims. Tighter latency targets require stable dedicated hardware and a larger representative corpus.

## Implementation

Updated `.github/workflows/linux-ci.yml` with `Acceptance test large-index performance and memory` in the existing `linux-core` job. The step:

- creates 50,000 synthetic sensor capture paths using Python from the stock Ubuntu runner;
- builds a persistent Lertaro index through the Release CLI already built earlier in the job;
- searches for the last generated capture and verifies the expected result;
- records elapsed wall-clock time and peak RSS using standard Linux tooling;
- fails CI when any acceptance ceiling is exceeded.

No production code or dependencies were added.

## Validation

The workflow change itself triggers `Linux Port CI` on `linux-port`. This milestone is accepted only when the complete matrix remains green and the new step prints measured index/search timing and RSS values.

## Next

If this gate passes, finish Stage 7's plugin-boundary decision, synchronize roadmap status with delivered work, add final release/user documentation, and perform the formal release acceptance pass including the aggregate test suite permitted by `AGENTS.md` only during Release Flow.
