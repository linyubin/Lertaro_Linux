# 0021 — Release acceptance checklist and roadmap reconciliation

Date: 2026-09-18
Branch: `linux-port`

## Goal

Turn the remaining Ubuntu-port work into an explicit, auditable release gate and correct stale roadmap status before final acceptance.

## Decision

Do not infer completion from a green CI run alone. The port is complete only when every required item in `Linux/RELEASE_CHECKLIST.md` is satisfied. The checklist separates automated CI evidence from desktop-environment checks that cannot be honestly represented by a headless runner.

Stage 8 is now marked `in progress` because self-contained publishing, Debian packaging, real x64 install/use/remove smoke testing, and Xvfb graphical startup have already landed and passed CI. Stage 7 remains `in progress` until plugin-boundary documentation and representative large-index performance acceptance are closed.

## Implementation

Added `Linux/RELEASE_CHECKLIST.md` covering:

- build/test and final release matrix;
- daemon/index recovery;
- x64/arm64 packaging;
- performance and memory acceptance;
- GNOME/KDE desktop integration evidence;
- search feature parity and plugin boundary;
- installation, limitations, and final sign-off documentation.

Updated `DevelopmentLogs/ROADMAP.md` so Stage 8 reflects the implemented release work and points to the checklist for remaining gates.

## Validation

Documentation-only change. No production code changed, so no MSTest or narrow project build is required under `AGENTS.md`. The immediately preceding production/CI milestone, graphical Avalonia startup acceptance, is green in Linux Port CI run #46.

## Next

Close the highest-value unchecked automated gates first: packaged restart/corruption recovery and representative large-index performance/memory acceptance. Then document the plugin boundary and user-facing Ubuntu lifecycle, run the formal final release matrix, and record exact sign-off evidence.