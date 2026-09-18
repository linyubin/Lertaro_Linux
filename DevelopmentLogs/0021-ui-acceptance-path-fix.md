# 0021 — Graphical UI acceptance path fix

Date: 2026-09-18
Branch: `linux-port`

## Goal

Restore the Stage 8 graphical UI acceptance gate after Linux Port CI run #45 failed before exercising Avalonia.

## Diagnosis

The desktop project builds its managed entry assembly as `lertaro-ui.dll`, as shown by the successful Release build output. The newly added Xvfb smoke test attempted to execute `Linux.App.dll`, which does not exist. The resulting failure was therefore a CI harness defect rather than an application startup failure.

## Implementation

Updated `.github/workflows/linux-ci.yml` so the Xvfb launch gate executes:

`Linux/App/bin/Release/net10.0/lertaro-ui.dll`

The existing acceptance rule remains unchanged: the process must remain alive for the five-second window and be terminated by `timeout` (status 124). An early process exit still fails the gate and prints the captured application log.

## Validation

The workflow change triggers Linux Port CI. Acceptance requires the `linux-core` job to pass the graphical launch gate and then continue through direct scan, persistent-index, daemon/watch, restart/corrupt-index recovery, and large-index performance checks. The publish and Debian package matrices must remain green.

## Next

Once CI is green, reconcile the roadmap with the milestones already implemented, complete the plugin-boundary/release documentation work, and execute the final release checklist before declaring the Ubuntu port complete.
