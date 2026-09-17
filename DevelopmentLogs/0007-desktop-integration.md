# Step 0007 — Linux desktop activation integration

Date: 2026-09-17
Branch: `linux-port`

## Goal

Make search results actionable using Linux desktop standards without reproducing Windows Explorer hooks.

## Decisions

- Open files and directories through `xdg-open` with `UseShellExecute=false` and argument-list APIs; no user path is interpolated into a shell command.
- Reveal files through the freedesktop `org.freedesktop.FileManager1.ShowItems` D-Bus interface using `gdbus` when available.
- Fall back to opening the containing directory when the FileManager1 service/client is unavailable.
- Do not implement arbitrary global key capture under Wayland. The packaged desktop entry can be assigned to a GNOME/KDE system shortcut; this follows the compositor security model instead of attempting a fragile hook workaround.

## Implemented

- Added `LinuxDesktopActions.Open`.
- Added `LinuxDesktopActions.Reveal` with FileManager1 and directory fallback.
- Added command-construction tests proving paths are passed as discrete process arguments rather than shell text.
- Added `lertaro.desktop` packaging metadata.
- Wired Enter and Ctrl+Enter result activation into the Avalonia UI.

## Compatibility notes

`xdg-open` is the baseline freedesktop opener. `gdbus`/FileManager1 is supported by common Linux file managers; where it is unavailable the application still opens the containing directory. Desktop-global keyboard shortcut configuration is intentionally delegated to the desktop environment.

## Next step

Improve query semantics beyond filename-only fuzzy matching, then move to packaging and release acceptance.
