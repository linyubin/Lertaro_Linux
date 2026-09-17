# Step 0005 — Per-user daemon and Unix socket IPC

Date: 2026-09-17
Branch: `linux-port`

## Goal

Move index ownership into a long-lived per-user process so desktop clients can search a live index without repeatedly loading files or competing to write state.

## Decisions

- Use a normal per-user daemon (`lertarod`), not a privileged root service. User search data and filesystem access should stay within the logged-in user's permission boundary.
- Use an AF_UNIX stream socket with a small JSON line protocol. This keeps IPC local, inspectable and dependency-free.
- Restrict the runtime directory to mode 0700 and the socket to 0600 on Linux.
- Keep one request per connection for the first protocol version. Search clients are short-lived and this avoids connection/session state while the API is still evolving.
- Make the daemon the single writer of the persistent index. The CLI/UI request rebuild/search/status through IPC.
- Use XDG state/runtime locations when available, with deterministic home/temp fallbacks.
- Keep the recursive watcher enabled during reconciliation and serialize watcher event application behind a reconciliation gate. Events generated during a full scan queue and are applied after the rebuilt snapshot; an OS event overflow schedules another reconciliation.

## Implemented

- Added request/response/status/search-result IPC contracts and JSON framing.
- Added `LinuxDaemonClient`.
- Added `LinuxDaemonServer` with search, status, rebuild and shutdown commands.
- Added stale-socket detection and same-user socket permissions.
- Added XDG-aware `LinuxDaemonPaths`.
- Added `Linux/Daemon` producing the `lertarod` executable.
- Added CLI daemon commands: `daemon-search`, `daemon-status`, `daemon-rebuild`, `daemon-shutdown`.
- Added `LERTARO_SOCKET` override for testing and non-default sessions.
- Added a systemd user service unit template.
- Hardened watcher reconciliation against event-loss races.

## Tests

- Deterministic XDG path resolution tests.
- Real Unix-domain-socket server/client integration test covering status, fuzzy search, rebuild and graceful shutdown.
- Existing real filesystem watcher integration remains enabled.
- Ubuntu CI builds both CLI and daemon.
- CI launches a real daemon process, waits for IPC readiness, searches an initial file, creates a new file, verifies watcher-driven search visibility, rebuilds and performs IPC shutdown.

## Security boundary

The protocol is not network-accessible. On Linux the socket lives in a user-owned runtime directory and is chmod 0600. No request executes arbitrary shell commands. Search input is treated only as a fuzzy query, and rebuild operates only on the root configured when the daemon starts.

## Next step

Build the Avalonia desktop client against daemon IPC, then add file activation/reveal behavior and desktop-environment shortcut integration. UI work must not regain direct ownership of the index.
