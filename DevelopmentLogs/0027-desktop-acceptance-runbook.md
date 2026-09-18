# 0027 — Desktop acceptance runbook

Date: 2026-09-18
Branch: `linux-port`

## Goal

Turn the remaining GNOME/Wayland and KDE/Plasma release gates into a reproducible, auditable manual acceptance procedure instead of an informal visual check.

## Decision

Do not weaken the release checklist by treating Xvfb as compositor validation. CI already proves that the Release-built Avalonia application can initialize under X11, but it cannot prove desktop-menu launch, Wayland shortcut configuration, file-manager integration, login-session systemd behavior, or compositor-specific user interaction.

The manual gate therefore remains required for GNOME/Wayland. KDE/Plasma remains best-effort because the release target is Ubuntu; an unavailable KDE environment must be recorded explicitly rather than represented as a pass.

## Implementation

Added `Linux/DESKTOP_ACCEPTANCE.md`. The runbook binds evidence to an exact commit and package SHA256 and covers:

- package install and systemd user-service startup;
- real incremental create/rename/delete search behavior;
- desktop-menu UI launch and visible control sanity;
- file/directory activation and FileManager1 reveal behavior;
- history, bookmarks, and application launching;
- desktop-environment global shortcut behavior under Wayland's supported model;
- daemon restart and logout/login persistence;
- journal inspection and package uninstall;
- explicit PASS/FAIL/NOT AVAILABLE evidence fields.

## Validation

This change is documentation/test-procedure only and changes no production code. Under `AGENTS.md`, no project build, formatting pass, or MSTest run is warranted. The immediately preceding formal automated release matrix already passed on release-candidate commit `53a7f0261d7441b58c84c4d57137702db3051b37` in Linux Port CI run #55.

The desktop runbook itself cannot honestly be marked passed from a headless GitHub runner. Its purpose is to make the remaining real-desktop validation deterministic and reviewable.

## Next

Execute `Linux/DESKTOP_ACCEPTANCE.md` on a clean Ubuntu GNOME Wayland desktop using the release-candidate package. Record KDE/Plasma evidence where practical. If the required GNOME gate passes, update `Linux/RELEASE_CHECKLIST.md`, write the final release acceptance log with exact commit/package/CI evidence, mark Stage 8 complete, and stop development monitoring.
