# Lertaro Linux release acceptance checklist

This checklist is the final gate for declaring the Ubuntu port complete. A release candidate must satisfy every automated item and document any manual desktop-environment validation before release.

## Build and test

- [x] Linux Core, CLI, Daemon, App and targeted MSTest projects build/test on Ubuntu CI.
- [x] Release-built Avalonia application starts under an X11 display (Xvfb smoke gate).
- [ ] Final formal release test matrix passes from the release candidate commit.

## Runtime and recovery

- [x] Persistent index survives separate CLI processes.
- [x] Incremental filesystem changes are reconciled and persisted.
- [x] Per-user daemon IPC reconnect/restart behavior is covered.
- [x] Release-state restart/corruption recovery acceptance passes from packaged binaries.

## Packaging

- [x] Self-contained linux-x64 publish is produced.
- [x] Self-contained linux-arm64 publish is produced.
- [x] Debian packages are built and archive-validated for x64 and arm64.
- [x] x64 Debian package is installed with dpkg on Ubuntu CI.
- [x] Installed CLI performs a real indexed search.
- [x] Package-owned launcher, desktop entry and systemd user unit are present after install and removed after uninstall.

## Performance

- [x] Representative large synthetic index gate passes for build/load/search latency.
- [x] Peak memory acceptance is recorded and below the 512 MiB regression ceiling on the CI fixture.

## Desktop integration

- [x] File/folder activation uses freedesktop-compatible mechanisms.
- [x] File reveal uses org.freedesktop.FileManager1 when available.
- [x] Global-shortcut integration avoids unsupported Wayland-wide hooks.
- [ ] Manual GNOME/Wayland smoke test recorded on a clean supported Ubuntu desktop.
- [ ] Manual KDE/Plasma smoke test recorded where practical; limitations documented if unavailable.

## Feature parity

- [x] Path-aware query terms and filters.
- [x] Search history/recent results.
- [x] Persistent bookmarks/favorites with CLI/daemon/UI integration.
- [x] Linux application catalog/launcher with daemon/UI integration.
- [x] Plugin boundary review completed and the supported cross-platform subset documented in `Linux/PLUGIN_COMPATIBILITY.md`.

## Documentation and release

- [x] User-facing Ubuntu install/start/uninstall instructions are complete in `Linux/README.md`.
- [x] Known limitations are documented in `Linux/README.md` and `Linux/PLUGIN_COMPATIBILITY.md`.
- [x] `DevelopmentLogs/ROADMAP.md` reflects actual stage status.
- [ ] Final release acceptance log records the exact CI run/commit used for sign-off.

Do not declare the Ubuntu port complete while any required item above remains unchecked.