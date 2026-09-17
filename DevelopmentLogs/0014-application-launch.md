# 0014 — Linux application launch

Date: 2026-09-18
Branch: `linux-port`

## Goal

Close the execution half of the Stage 7 application-launcher milestone. Application discovery and daemon enumeration already existed, but discovered desktop entries could not yet be launched by Linux front ends.

## Decision

Launch freedesktop desktop entries through `gio launch <desktop-file>` rather than parsing the `Exec=` field ourselves. Desktop-entry `Exec` supports field codes and quoting rules that are easy to implement incorrectly; delegating launch semantics to GLib keeps Lertaro out of command-line parsing and avoids invoking a shell.

The launcher accepts only `.desktop` paths and passes the path through `ProcessStartInfo.ArgumentList` with `UseShellExecute=false`.

## Implementation

- Added `LinuxDesktopActions.LaunchApplication` and `BuildLaunchApplicationStartInfo`.
- Added CLI `apps [query] [limit]` backed by the daemon's `application-list` command.
- Added CLI `app-launch <desktop-file>` for a direct, scriptable launcher path.
- Added tests verifying `gio launch` argument isolation and rejection of non-desktop files.

## Validation

The changed Core and Tests projects are covered by the existing Ubuntu Release CI gate. Each push to `linux-port` triggers build, MSTest, daemon smoke checks and CLI smoke checks.

## Remaining

Expose application results and activation in the Avalonia search UI, then continue Stage 7 performance instrumentation and plugin-boundary review before Stage 8 packaging/release acceptance.
