# 0011 — Persistent bookmarks

Date: 2026-09-17
Branch: `linux-port`

## Goal

Advance Stage 7 search parity with a small, UI-independent persistent favorites primitive that can later be exposed by the daemon, CLI and Avalonia UI.

## Decision

Bookmarks are stored as normalized absolute paths in a JSON preference file. The core class owns persistence so every front end observes the same semantics. Writes use a sibling temporary file followed by replacement to avoid leaving a partially-written preferences file after interruption.

The path set uses ordinal comparison. Linux filesystems are generally case-sensitive; applying Windows-style case folding would make two valid Linux paths indistinguishable.

A corrupt bookmarks preference file is treated as empty rather than making the search application fail to start. This is intentionally different from the index snapshot, whose integrity is operationally significant and can trigger rebuild/recovery.

## Implementation

- Added `LinuxBookmarks` to `Linux/Core`.
- Supports idempotent `Add`, `Remove` and `Contains` operations.
- Persists immediately after mutations.
- Normalizes paths with `Path.GetFullPath`.
- Added MSTest coverage for persistence, idempotency, removal and corrupt-file recovery.

## Validation

Automated Ubuntu CI is triggered by each push to `linux-port`. The bookmark tests are part of `Linux/Tests` and therefore run in the existing Release test gate.

## Next

Expose bookmark operations through the daemon protocol and UI after the core persistence primitive is green. Continue Stage 7 with launcher sources and performance acceptance instrumentation.
