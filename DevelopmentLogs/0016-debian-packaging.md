# 0016 — Debian packaging

Date: 2026-09-18
Branch: `linux-port`

## Goal

Advance Stage 8 from raw self-contained publish outputs to an installable Debian package for Ubuntu-class systems.

## Decision

Use the platform-native `dpkg-deb` tool rather than adding a packaging dependency. The package keeps the three self-contained .NET applications under `/usr/lib/lertaro`, exposes stable launch commands through `/usr/bin`, installs the desktop entry, and installs a per-user systemd unit under `/usr/lib/systemd/user`.

The package-specific systemd unit uses `/usr/lib/lertaro/lertarod`. The existing development/user-local unit remains unchanged because its `%h/.local/lib/lertaro` path is still useful for non-package installs.

Both `amd64` and `arm64` packages are built from their matching self-contained runtime outputs.

## Implementation

- Added `Linux/Packaging/build-deb.sh`.
- Maps `linux-x64` to Debian `amd64` and `linux-arm64` to Debian `arm64`.
- Combines CLI, daemon and Avalonia app publish outputs into one package payload.
- Adds `/usr/bin/lertaro` and `/usr/bin/lertaro-ui` symlinks.
- Installs `lertaro.desktop` and a package-specific systemd user service.
- Extended Linux CI with a `linux-deb` matrix for x64 and arm64.
- CI verifies package metadata/content and required daemon, desktop and systemd paths.

## Validation

GitHub Actions Linux Port CI run #41 was triggered for the complete packaging workflow. At the time this log was written it was still executing, so this milestone is not considered green until that run completes successfully. The next development pass must inspect the result and fix any packaging failure before advancing.

## Next

After the package gate is green, add install/uninstall lifecycle validation and clean-environment smoke coverage, then continue the remaining Stage 7 plugin-boundary/performance work and Stage 8 release acceptance.
