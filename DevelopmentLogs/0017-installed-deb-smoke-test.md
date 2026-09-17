# 0017 — Installed Debian package smoke test

Date: 2026-09-18
Branch: `linux-port`

## Goal

Advance Stage 8 release acceptance from package-archive inspection to an actual install/use/remove cycle on Ubuntu.

## Decision

The executable smoke test runs only for the `linux-x64` Debian package because the GitHub-hosted Ubuntu runner is x64 and cannot natively execute the arm64 payload. The arm64 package remains covered by self-contained publish and Debian archive validation.

The acceptance test deliberately uses `dpkg --install` and `dpkg --remove` rather than only extracting the archive. This verifies package metadata, filesystem placement, command symlinks, desktop entry placement, and the systemd user-unit installation path as Ubuntu would see them.

A graphical UI launch is not attempted in this headless gate. Avalonia compilation/publish is already validated separately; interactive display-server acceptance belongs to a GUI-capable clean-machine test.

## Implementation

Updated `.github/workflows/linux-ci.yml` so the x64 Debian job now:

- installs the generated package with `dpkg`;
- verifies the installed CLI, UI launcher, daemon, systemd user unit and desktop entry;
- executes the installed `lertaro` CLI against the packaged payload and verifies a search result;
- removes the package;
- verifies package-owned launchers/integration files are removed.

## Validation

The push triggers `Linux Port CI`. This milestone is accepted only if the new `linux-deb (linux-x64)` job completes successfully together with the existing core, publish, arm64 package, and unit/integration test matrix.

## Next

Add release-state recovery/restart acceptance and representative large-index performance/memory gates. Then add release documentation and a final clean-machine acceptance checklist before declaring the Ubuntu port complete.
