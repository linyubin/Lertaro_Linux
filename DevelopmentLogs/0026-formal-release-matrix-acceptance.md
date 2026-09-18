# 0026 — Formal release matrix acceptance

Date: 2026-09-18
Branch: `linux-port`

## Goal

Close the automated final-release-matrix gate with auditable evidence from the release-candidate workflow rather than inferring readiness from individual jobs.

## Decision

Accept commit `53a7f0261d7441b58c84c4d57137702db3051b37` as the automated release-candidate baseline because Linux Port CI run #55 completed successfully with the dedicated `release-matrix` gate requiring the Linux core/test, self-contained publish, and Debian package matrices all to succeed.

Do not declare the Ubuntu port complete yet. The remaining checklist items are desktop-environment acceptance on a clean GNOME/Wayland system and, where practical, KDE/Plasma. Xvfb proves graphical process startup but is not evidence for compositor-specific shortcut, file-manager, or session integration.

## Evidence

Linux Port CI run #55 (`35334101585`) completed successfully for commit `53a7f0261d7441b58c84c4d57137702db3051b37`.

The formal gate aggregates:

- Release builds and targeted MSTest coverage for Linux Core/CLI/Daemon/App;
- graphical Avalonia startup under Xvfb;
- direct scan and persistent-index smoke tests;
- daemon IPC, watcher, restart, and corrupt-index recovery acceptance;
- 50,000-file performance and memory regression acceptance;
- self-contained `linux-x64` and `linux-arm64` publish for CLI, daemon, and App;
- x64/arm64 Debian package construction and archive validation;
- real x64 Debian install/search/recovery/uninstall validation, including the packaged systemd user unit.

## Repository update

Updated `Linux/RELEASE_CHECKLIST.md` to mark the formal release matrix as passed and record the exact release-candidate commit and CI run.

No production code changed. Under `AGENTS.md`, no additional build, formatting, or MSTest execution is required for this documentation-only acceptance record.

## Remaining release gates

1. Record a clean Ubuntu GNOME/Wayland manual smoke test covering package installation, user daemon lifecycle, search UI startup, file/folder activation, file reveal, and configured global shortcut behavior.
2. Record a KDE/Plasma smoke test where practical, or document why that environment was unavailable and the resulting support limitation.
3. After those environment-specific gates are closed, run/confirm the final candidate matrix at the sign-off commit, write the final acceptance log, mark Stage 8 complete, and only then declare the Ubuntu port complete.
