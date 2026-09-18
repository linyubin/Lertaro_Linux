# 0022 — Packaged daemon recovery acceptance

Date: 2026-09-18
Branch: `linux-port`

## Goal

Close the gap between source-tree recovery testing and the actual Debian payload shipped to Ubuntu users.

## Decision

Exercise restart/corrupt-index recovery through the installed x64 package in the existing Debian smoke job. This reuses the package install boundary and native self-contained executables, avoiding a second packaging harness.

The arm64 package remains archive/publish validated because the hosted runner cannot execute it natively.

## Implementation

Updated `.github/workflows/linux-ci.yml` so the installed-package smoke test now:

- creates an isolated root, index and Unix socket;
- starts `/usr/lib/lertaro/lertarod` from the installed package;
- waits for readiness through the installed `/usr/bin/lertaro` CLI;
- verifies a known file is searchable;
- shuts down cleanly and confirms a persistent index was written;
- deliberately corrupts that index;
- restarts the packaged daemon and requires automatic recovery to restore the known search result;
- shuts down cleanly before the existing package uninstall checks.

No production code changed, so no new MSTest coverage is required.

## Validation

The workflow update triggers `Linux Port CI`. This milestone is accepted only if the `linux-deb (linux-x64)` job passes the new installed-binary recovery path and the rest of the existing matrix remains green.

## Next

After CI is green, update the release checklist to record packaged recovery and the already-green large-index performance gate. Then complete user-facing Ubuntu lifecycle/limitations documentation and proceed toward the formal release matrix. Manual GNOME/Wayland and KDE desktop evidence remains a separate release gate.