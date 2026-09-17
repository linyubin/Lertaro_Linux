# 0015 — Application launcher UI integration

Date: 2026-09-18
Branch: `linux-port`

## Goal

Complete the user-facing portion of the Stage 7 Linux application launcher by exposing daemon-discovered desktop applications in the Avalonia search window and activating them without shell command parsing.

## Decision

Application search uses an explicit `>` prefix in the main query box. This keeps filesystem search behavior unchanged while providing a fast launcher mode familiar from command-palette interfaces. The UI consumes a unified `LinuxDesktopResult` model so keyboard navigation and activation remain shared between filesystem and application results.

Application activation continues to delegate freedesktop desktop-entry semantics to `gio launch`, implemented in the previous milestone. Ctrl+Enter reveal semantics remain filesystem-only.

## Implementation

- Added `LinuxDesktopResult` and `LinuxDesktopResultKind` in Linux Core.
- Added adapters from daemon filesystem and application response records.
- Updated Avalonia search to issue `application-list` when the query begins with `>`.
- Updated the result template to render the unified result model.
- Enter/double-click now launches application results and opens filesystem results as before.
- Added MSTest coverage for result mapping and activation target preservation.

## Validation

Every push to `linux-port` triggers the Ubuntu Release CI gate, including Core/App builds, Linux MSTest, daemon smoke checks and CLI smoke checks. This milestone is considered complete only when the resulting CI run is green.

## Remaining

Continue Stage 7 with performance instrumentation/acceptance and plugin-boundary review. After Stage 7 closes, proceed to self-contained x64/arm64 publishing, `.deb` packaging, install/uninstall lifecycle tests and final release acceptance.
