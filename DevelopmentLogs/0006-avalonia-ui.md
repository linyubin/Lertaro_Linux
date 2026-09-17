# Step 0006 — Avalonia desktop search UI

Date: 2026-09-17
Branch: `linux-port`

## Goal

Provide a native Linux desktop search window that consumes the live daemon index instead of loading or mutating index files itself.

## Decision

Use Avalonia 12.1.2 on .NET 10. Avalonia is the current cross-platform .NET desktop framework and preserves the C#/XAML development model used by the upstream WPF application without coupling the Linux port to GTK or KDE APIs.

The first UI deliberately stays narrow: one search box, keyboard-oriented result list and status line. Index ownership remains exclusively in `lertarod`.

## Implemented

- Added `Linux/App` targeting `net10.0` and Avalonia 12.1.2.
- Added a Fluent-themed desktop window.
- Queries are sent asynchronously to the Unix-socket daemon.
- Superseded query responses are ignored with a generation counter, preventing stale results from replacing newer input.
- Result navigation supports Down, Enter and Escape.
- Ctrl+Enter reveals a file; Enter opens the selected file or directory.
- Daemon connection failures are surfaced in the status line rather than terminating the UI.

## Validation

- Ubuntu CI builds the Avalonia application in Release mode.
- Search transport and daemon behavior remain covered by the Linux Core integration tests.
- Pure UI/XAML glue is intentionally not unit-tested under the repository's test rules; behavior-bearing search/index code remains outside the UI project.

## Next step

Add freedesktop file activation/reveal behavior and package the application with a desktop entry. Global activation will use desktop-environment shortcuts rather than unsupported Wayland-wide keyboard hooks.
