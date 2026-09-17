# 0010 — Persistent search history

Date: 2026-09-17
Branch: `linux-port`

## Goal

Advance Stage 7 search parity by adding a small cross-platform persistent search-history primitive that can be consumed by the CLI and Avalonia UI without coupling either surface to storage details.

## Decisions

- Store history as JSON in a caller-selected path so XDG path policy remains outside the storage primitive.
- Keep at most 100 entries by default, ordered by most recent use.
- Deduplicate exact queries and increment `UseCount` instead of accumulating duplicate rows.
- Ignore blank queries.
- Serialize mutation through `SemaphoreSlim` because daemon/UI requests may record history concurrently.
- Persist through a sibling temporary file followed by replacement to avoid exposing partially-written JSON after process interruption.

## Production changes

Added `LinuxSearchHistory` and `LinuxSearchHistoryEntry` in `Linux/Core`.

The API supports asynchronous load and record operations, bounded retention, usage counts, and deterministic timestamp injection for tests.

## Validation

Added `LinuxSearchHistoryTests` covering:

1. persistence across a new history-store instance;
2. duplicate query usage-count increment;
3. recency ordering;
4. capacity eviction;
5. blank-query suppression.

Ubuntu CI is expected to run the existing Release build/test/smoke pipeline for this commit series. CI result must be green before this milestone is considered accepted.

## Next

Wire history recording/reading into daemon IPC and desktop view-model behavior, then implement favorites/bookmarks as the next persistent user-data primitive.
