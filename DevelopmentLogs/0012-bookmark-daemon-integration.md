# 0012 — Bookmark daemon integration

Date: 2026-09-17
Branch: `linux-port`

## Goal

Continue Stage 7 by making persistent bookmarks available through the single-writer daemon boundary instead of leaving the feature as an isolated core preference class.

## Decision

The daemon owns the `LinuxBookmarks` instance and exposes three explicit IPC commands: `bookmark-list`, `bookmark-add`, and `bookmark-remove`. Bookmark mutations are therefore serialized through the same per-user process that owns live index state. The protocol request gains an optional `Path` field rather than overloading the search `Query` field, keeping command semantics explicit and leaving room for future bookmark metadata.

The bookmark file is stored beside the configured index (`bookmarks.json`). This preserves isolation when callers override the daemon index path for testing or alternate profiles, while the default installation naturally places both files under the XDG state directory.

## Implementation

- Extended `LinuxDaemonRequest` with an optional bookmark path.
- Extended `LinuxDaemonResponse` with a bookmark path collection.
- Added daemon commands to list, add and remove bookmarks.
- Wired `LinuxBookmarks` into `lertarod` startup.
- Added daemon integration coverage that verifies add/list/remove round trips and on-disk persistence.
- Added validation for missing bookmark paths.

## Validation

The change is covered by `LinuxDaemonServerTests` and the existing `LinuxBookmarksTests`. The `linux-port` push workflow builds the Linux Release projects and runs the complete Linux MSTest project on Ubuntu. CI results must be green before the next Stage 7 milestone is treated as complete.

## Next

Expose bookmark state/actions in the Avalonia view model and UI, then continue Stage 7 with Linux application launcher discovery and representative-index performance instrumentation.
