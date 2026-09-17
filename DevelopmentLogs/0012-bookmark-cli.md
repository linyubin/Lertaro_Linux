# 0012 — Bookmark CLI integration

Date: 2026-09-18
Branch: `linux-port`

## Goal

Complete the command-line surface for the persistent bookmark capability already owned by the Linux daemon, so bookmarks can be exercised without the desktop UI and included in end-to-end Ubuntu validation.

## Decision

The CLI remains a thin daemon client. It does not open the bookmark preference file directly, preserving the daemon as the single writer for user state and avoiding competing persistence paths.

The three commands intentionally mirror the daemon protocol names: `bookmark-list`, `bookmark-add`, and `bookmark-remove`.

## Implementation

- Added `bookmark-list` to print all persisted bookmark paths.
- Added `bookmark-add <path>` and `bookmark-remove <path>`.
- Mutations print the resulting bookmark set, which is useful for both shell use and deterministic smoke testing.
- Extended CLI usage text.
- Extended the existing daemon smoke test to add, list, and remove a bookmark through the CLI.

## Validation

The changed CLI and workflow trigger `Linux Port CI` on Ubuntu. The workflow builds the narrow Linux CLI, daemon and desktop app projects, runs `Linux/Tests`, exercises daemon search/watcher behavior, and now verifies the bookmark CLI round trip through the real Unix-domain-socket daemon.

The container available to this development run cannot resolve github.com, so local clone/build/format execution is unavailable. Repository-side Ubuntu CI is therefore the executable validation source for this step.

## Next

- expose bookmark state and toggle actions in the Avalonia UI;
- update the roadmap to reflect completed Stages 2–6 and current Stage 7 work;
- continue Stage 7 performance acceptance instrumentation before packaging/release acceptance.
