# 0013 — Self-contained Linux publish validation

Date: 2026-09-18
Branch: `linux-port`

## Goal

Start Stage 8 release acceptance by proving that the Linux CLI, daemon, and Avalonia desktop app can all be published as self-contained binaries for the two planned architectures: `linux-x64` and `linux-arm64`.

## Decision

Keep publish validation in the existing Ubuntu workflow rather than adding a separate release workflow before packaging exists. A matrix covers the three executable projects and two runtime identifiers, so every architecture/project combination fails independently and is visible in CI.

This step does not create release artifacts or bump project versions. Repository rules reserve version changes for the formal release flow, and packaging is not yet ready for distribution.

## Implementation

- Added a `linux-publish` CI job.
- Publishes `Linux/Cli`, `Linux/Daemon`, and `Linux/App` in Release mode.
- Validates both `linux-x64` and `linux-arm64`.
- Uses `--self-contained true` so target machines do not require a preinstalled .NET runtime.
- Verifies that each publish directory contains an executable native launcher.

## Validation

The workflow itself is the narrow validation target because this change modifies only CI/release orchestration. The existing `linux-core` job remains unchanged and continues to build the three projects, run `Linux/Tests`, and exercise direct scan, persistent index, daemon IPC, and incremental watcher smoke tests.

The new matrix must be green before `.deb` packaging work begins. A cross-architecture publish does not execute ARM64 binaries on the x64 runner; clean-machine/runtime execution remains part of later Stage 8 acceptance.

## Next

Add deterministic Debian package assembly around the validated self-contained publish outputs, including desktop entry and per-user systemd service installation. Then add install/uninstall and clean-environment smoke tests.
