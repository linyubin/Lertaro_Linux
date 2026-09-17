# Step 0003 — Persistent Linux index

Date: 2026-09-17
Branch: `linux-port`

## Goal

Remove the mandatory full filesystem scan from every search invocation and establish a versioned, crash-resistant persistence boundary for Linux indexing.

## Decisions

- Use a compact dependency-free binary format for the Linux MVP instead of JSON or SQLite. The index can contain millions of rows, so JSON would add avoidable parse and storage overhead; SQLite is deferred until a demonstrated need for transactional/query semantics.
- Store paths relative to the configured root and reconstruct absolute paths on load.
- Reject rooted, parent-traversal and out-of-root paths while writing or loading an index.
- Write to a same-directory temporary file, flush it to disk, then atomically rename over the target.
- Preserve the original direct-scan CLI mode while adding explicit `index` and `search` commands.

## Implemented

- Added `LinuxIndexSnapshot` as the platform-neutral persisted-index boundary.
- Added `LinuxIndexStore` format v1 with magic/version validation, entry-count sanity checks, path-containment validation and atomic replacement.
- Added `lertaro-linux index <root> <index-file>`.
- Added `lertaro-linux search <index-file> <query> [limit]`.
- Added round-trip, invalid-magic and out-of-root persistence tests.
- Extended Ubuntu CI with an end-to-end persistent-index smoke test.

## Validation

Automated validation is performed by `.github/workflows/linux-ci.yml` on Ubuntu:

1. build Linux CLI in Release mode;
2. run the Linux MSTest project;
3. direct-scan CLI smoke test;
4. create a temporary filesystem tree;
5. persist it to an index file;
6. launch a separate CLI process to load that index and verify fuzzy search output.

## Known limitations

- The whole persisted index is still loaded into managed memory for search.
- The format currently stores only path, directory flag and logical size.
- File changes after index creation are not yet reflected until a rebuild.

## Next step

Add an in-process mutable catalog and `FileSystemWatcher`-backed incremental updates. On Linux, .NET's watcher maps to inotify; watcher overflow/error will deliberately trigger a full reconciliation rather than silently trusting a potentially incomplete event stream.
