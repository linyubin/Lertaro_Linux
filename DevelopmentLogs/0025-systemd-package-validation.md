# 0025 — Packaged systemd user-unit validation

Date: 2026-09-18
Branch: `linux-port`

## Goal

Close a packaging acceptance blind spot before final Ubuntu release sign-off: the Debian smoke test previously verified that the systemd user unit existed, but did not validate its syntax or prove that its `ExecStart` target matched the installed package payload.

## Decision

Keep the gate dependency-free and use Ubuntu's existing `systemd-analyze verify` rather than introducing a custom parser. Also assert the exact packaged `ExecStart=/usr/lib/lertaro/lertarod` line so a stale development/user-local service template cannot silently enter the Debian package.

This is deliberately narrower than claiming a real GNOME user-session lifecycle test. GitHub-hosted headless runners do not provide a representative logged-in GNOME/Wayland user session, so compositor/session-specific acceptance remains manual and must not be marked complete from this gate.

## Implementation

Updated `.github/workflows/linux-ci.yml` in the installed `linux-x64` Debian smoke test to:

- run `systemd-analyze verify --user` against the installed unit;
- require the unit's `ExecStart` to reference `/usr/lib/lertaro/lertarod` exactly;
- retain the existing direct execution, daemon IPC, persistent-index corruption recovery, and uninstall checks.

No production C# logic changed, so no MSTest additions or project formatting are required under `AGENTS.md`.

## Validation

The workflow update triggers `Linux Port CI` on `linux-port`. This milestone is accepted only when the complete existing CI matrix remains green and the x64 Debian job passes the new systemd verification gate.

## Next

If CI is green, the remaining non-automated release blockers are the clean GNOME/Wayland desktop smoke test and, where practical, KDE/Plasma validation. After those are recorded, run the formal release-candidate matrix and write the exact commit/run sign-off log before declaring the Ubuntu port complete.
