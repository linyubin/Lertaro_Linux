# 0012 — Linux application catalog

Date: 2026-09-18
Branch: `linux-port`

## Goal

Advance Stage 7 search parity by discovering desktop applications from the standard freedesktop data directories without adding a dependency or coupling discovery to the Avalonia UI.

## Decision

Use freedesktop `.desktop` files as the launcher source. The catalog reads `$XDG_DATA_HOME/applications` first, followed by `$XDG_DATA_DIRS/applications`, with the standard Linux defaults when those variables are unset.

Desktop IDs are derived from paths relative to the `applications` directory. A higher-priority desktop ID is marked as seen before visibility filtering so a user `Hidden=true` entry correctly masks a system entry with the same ID. `NoDisplay=true`, non-`Application` entries, and entries without `Name` or `Exec` are excluded.

This step intentionally discovers launchable applications but does not execute the `Exec` field. Desktop-entry command expansion has quoting and field-code semantics; launching will use the desktop environment's application activation mechanism rather than implementing an unsafe partial parser.

## Implementation

- Added `LinuxApplicationCatalog` and `LinuxApplicationEntry` to `Linux/Core`.
- Added XDG data-directory discovery with standard defaults.
- Added desktop-ID de-duplication and user override/masking semantics.
- Added minimal `[Desktop Entry]` parsing for `Type`, `Name`, `Exec`, `Icon`, `Hidden`, and `NoDisplay`.
- Added focused MSTest coverage for visibility, ordering, masking, nested desktop IDs, required `Exec`, and localized-key handling.

## Validation

The changed source and test files are below the repository's 300-line limit. `Linux/Tests/Linux.Tests.csproj` is the narrow test target. Ubuntu CI is the authoritative build/test environment because this automation runtime has no outbound Git clone access; any CI failure blocks further Stage 7 work.

## Next

Expose application results through the daemon search protocol and Avalonia result model, then activate selected desktop IDs through a freedesktop-compatible launcher. After that, continue with performance acceptance instrumentation and the plugin boundary review.
