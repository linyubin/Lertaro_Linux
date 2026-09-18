# Lertaro Linux port roadmap

Date: 2026-09-18
Branch: `linux-port`

This file defines the ordered delivery plan. Each stage gets a numbered development log and its own automated validation before work advances.

## Stage 1 — Feasibility and vertical slice

Status: completed.

- isolate Linux-specific `net10.0` projects;
- reuse upstream fzf implementation;
- scan Linux filesystems;
- execute CLI fuzzy search;
- establish Ubuntu CI.

## Stage 2 — Persistent index

Status: completed.

- versioned persistent index;
- atomic writes and corruption checks;
- CLI index build/load/search;
- integration tests across separate processes.

## Stage 3 — Incremental indexing

Status: completed.

- mutable in-memory catalog;
- recursive `FileSystemWatcher`/inotify event ingestion;
- create/change/delete/rename handling;
- debounced persistence;
- watcher overflow detection and full reconciliation;
- integration tests on Ubuntu.

## Stage 4 — User daemon and IPC

Status: completed.

- per-user background daemon rather than a privileged SYSTEM-equivalent service;
- single-writer ownership of index state;
- Unix-domain-socket request protocol;
- search, status, rebuild and shutdown requests;
- systemd user unit;
- reconnect/restart tests.

## Stage 5 — Desktop search UI

Status: completed.

- Avalonia desktop application;
- instant query/result interaction against daemon IPC;
- keyboard navigation and result activation;
- path display, file/folder distinction and basic settings;
- UI-independent view-model tests plus Linux build validation.

## Stage 6 — Linux shell integration

Status: completed.

- open files/directories through freedesktop-compatible mechanisms;
- reveal selected files through `org.freedesktop.FileManager1` where available;
- configurable desktop-environment global shortcut to show/toggle search;
- avoid unsupported Wayland-wide key hooks;
- GNOME/KDE compatibility validation where automation is practical.

## Stage 7 — Search feature parity

Status: completed.

- path-aware query terms and filters;
- history/recent results;
- bookmarks/favorites;
- application launcher sources;
- plugin compatibility boundary and supported Linux subset documented in `Linux/PLUGIN_COMPATIBILITY.md`;
- performance regression gate using a representative synthetic index.

The initial Ubuntu release intentionally does not claim binary compatibility with Windows/Flow Launcher plugins. Future Linux plugin work is isolated from the release-critical daemon/index path.

## Stage 8 — Packaging and release acceptance

Status: in progress.

Completed acceptance work already includes self-contained x64/arm64 publish, Debian packaging/archive validation, real x64 package install/search/uninstall smoke testing, restart/corrupt-index recovery, large-index performance/memory regression testing, and graphical Avalonia startup under Xvfb. Remaining release gates are tracked in `Linux/RELEASE_CHECKLIST.md`.

- self-contained x64 and arm64 Linux publish;
- `.deb` packaging first, additional packaging only when justified;
- installation/uninstallation scripts and systemd user-unit lifecycle;
- clean-machine smoke test;
- persistence/restart/recovery tests;
- performance and memory acceptance checks;
- documentation and release checklist.

## Quality gates

A stage is not considered complete until:

- changed production logic has appropriate MSTest coverage;
- the narrow affected projects build in Release mode;
- Ubuntu CI is green;
- a numbered `DevelopmentLogs/` entry records design decisions, validation and remaining limitations;
- regressions discovered by CI are fixed before the next architectural stage starts.
