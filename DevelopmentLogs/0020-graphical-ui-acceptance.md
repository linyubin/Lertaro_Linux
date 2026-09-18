# 0020 — Graphical UI launch acceptance

Date: 2026-09-18
Branch: `linux-port`

## Goal

Close the remaining automated GUI launch gap in Stage 8 release acceptance.

## Decision

Use Xvfb on the Ubuntu CI runner rather than adding application-only test hooks or a second UI framework dependency. The acceptance criterion is deliberately narrow: the real Release-built Avalonia desktop application must initialize under an X11 display and remain alive for a five-second observation window.

A timeout exit code of 124 is success for this gate because the desktop application is expected to keep running until the user closes it. Any earlier exit is treated as a startup failure and its captured output is printed for diagnosis.

This validates process-level graphical startup without pretending that a virtual framebuffer proves GNOME/KDE interaction behavior. Desktop integration itself remains covered by the existing freedesktop-oriented implementation and tests.

## Implementation

Updated `.github/workflows/linux-ci.yml` to:

- install `xvfb` on the Ubuntu runner;
- launch the Release-built `Linux.App.dll` through `xvfb-run`;
- observe it for five seconds;
- fail if the UI exits or crashes before the observation window expires;
- print captured UI output on failure.

No production code changed, so no additional MSTest coverage is required for this step.

## Validation

The workflow update triggers `Linux Port CI`. This milestone is accepted only when the complete existing Linux CI matrix remains green together with the new graphical launch gate.

## Next

If CI is green, finish release documentation/checklist, reconcile ROADMAP status with implemented Stage 7/8 work, and run the final release acceptance matrix before declaring the Ubuntu port complete.
