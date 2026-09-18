# 0024 — Ubuntu user guide and release-gate reconciliation

Date: 2026-09-18
Branch: `linux-port`

## Goal

Close the user-facing lifecycle documentation gate and make the release checklist reflect automated acceptance work that has already passed.

## Decision

Keep the initial Ubuntu documentation operational and package-oriented rather than duplicating internal architecture notes. The supported path is the Debian package, per-user systemd service, XDG state locations, and the packaged Avalonia launcher. Desktop-environment-specific claims remain explicitly limited until manual GNOME/Wayland and KDE/Plasma smoke evidence exists.

The documentation does not claim Windows plugin compatibility, ARM64 runtime execution on GitHub's x64 runner, or Everything-class benchmark performance.

## Implementation

Added `Linux/README.md` with:

- amd64/arm64 package installation;
- systemd user-service enable/start/diagnostics;
- GUI and CLI startup examples;
- default XDG state/index/socket locations;
- supported custom-root override path;
- journal-based troubleshooting and corrupt-index recovery guidance;
- uninstall and optional user-state cleanup;
- explicit known limitations.

Reconciled `Linux/RELEASE_CHECKLIST.md` with already-green CI evidence: packaged restart/corruption recovery, the 50,000-entry performance regression gate, peak RSS ceiling, plugin-boundary documentation, roadmap status, and user-facing lifecycle/limitations documentation are now marked complete.

## Validation

Documentation-only change; no production code changed, so no MSTest or project build is required by `AGENTS.md`. The immediately preceding packaged recovery milestone is green in Linux Port CI run #50.

## Remaining release gates

- record a clean Ubuntu GNOME/Wayland manual smoke test;
- record KDE/Plasma smoke evidence where practical, or explicitly document why it is unavailable;
- execute the formal release-candidate test matrix allowed by `AGENTS.md` Release Flow;
- record the exact release commit and CI run in the final sign-off log.

The port must not be declared complete until those remaining checklist items are resolved.