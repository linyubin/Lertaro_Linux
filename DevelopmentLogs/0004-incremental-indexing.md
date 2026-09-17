# Step 0004 — Incremental Linux index updates

Date: 2026-09-17
Branch: `linux-port`

## Goal

Keep the persisted index current without rescanning the full tree for every filesystem change.

## Decisions

- Use .NET `FileSystemWatcher` rather than a custom inotify P/Invoke layer. On Linux it uses inotify and keeps the implementation inside the standard library.
- Keep the mutable catalog case-sensitive (`StringComparer.Ordinal`) because Linux path identity is case-sensitive even though user-facing fuzzy search remains case-insensitive.
- Treat directory create/rename-in events as subtree replacements. A directory can be moved into the watched root already containing files, so relying only on later child events would be incomplete.
- Treat watcher errors/overflow as loss of event-stream authority and perform a full reconciliation scan. Silent continuation after overflow is not accepted.
- Debounce persistence so bursts of filesystem events do not rewrite the index for every event.

## Implemented

- Added `LinuxMutableIndex` with thread-safe snapshot, single-path refresh, subtree replacement, subtree deletion and full replacement operations.
- Added `LinuxIndexWatcher` with recursive create/change/delete/rename processing.
- Added debounced atomic persistence through the existing `LinuxIndexStore`.
- Added full reconciliation after `FileSystemWatcher.Error`.
- Added watcher error state reporting through `LastError`.
- Added `lertaro-linux watch <index-file>` for a foreground validation/watch mode until the dedicated user daemon is introduced.

## Tests

- Subtree deletion does not accidentally remove same-prefix siblings.
- A pre-populated directory moved/created into the tree is indexed recursively.
- File metadata refresh updates size.
- Ubuntu integration test exercises real `FileSystemWatcher`: create -> rename directory -> recursive delete -> explicit persistence -> reload persisted index.

## Operational limits

- Linux inotify watch limits still apply to recursive `FileSystemWatcher`; hitting a kernel watch-resource limit can prevent or interrupt monitoring.
- The current foreground `watch` command is intentionally temporary. Stage 4 moves ownership to a per-user daemon and adds status/recovery IPC.
- Event processing and full reconciliation currently share the process; reconciliation of a very large tree can temporarily increase CPU and I/O.

## Next step

Introduce `lertarod` as the single writer for index state, expose search/status/rebuild over Unix-domain sockets, and ship a systemd user service definition. The UI and CLI will become clients of that daemon rather than loading/writing index files directly.
