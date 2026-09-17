# Step 0001 — Linux architecture audit

Date: 2026-09-17
Branch: `linux-port`

## Goal

Establish the minimum-risk path to an Ubuntu port before changing production code.

## Findings

- The existing `Core`, `Cli`, test projects and `PluginSdk` target `net10.0-windows`; `PluginSdk` also enables WPF.
- Windows-specific code is mixed into Core, including MFT/USN indexing, Win32 file enumeration, Explorer/file-dialog hooks and drive monitoring.
- The V2 snapshot/index/search implementation is valuable and mostly platform-neutral, but the current project boundary prevents compiling it on Linux without also compiling Windows-only code.
- The existing network-drive tree walk demonstrates a useful fallback indexing model, but its `NativeFileEnumerator` directly P/Invokes `kernel32.dll`.
- The low-level fzf implementation under `Core/SearchIndex/Fzf` is self-contained enough to reuse selectively from a Linux-specific project without copying source.

## Decision

Do not multi-target the existing Core yet. That would create a large conditional-compilation change before we have a runnable Linux baseline.

Create a narrow Linux vertical slice under `Linux/` that:

1. targets plain `net10.0`;
2. links the existing low-level fzf source files so search behavior starts from upstream code;
3. uses managed filesystem enumeration for the first scanner;
4. exposes a CLI to scan a selected root and search it;
5. has Linux-native tests and an Ubuntu GitHub Actions job.

Once this vertical slice is green, migrate reusable V2 snapshot/index components behind platform-neutral boundaries and replace the temporary in-memory scan list.

## Test plan

- Unit-test scanner recursion, hidden-dot-file handling and inaccessible/symlink behavior using temporary directories.
- Unit-test fuzzy matching/ranking against representative filenames.
- Run only the Linux test project and Linux CLI build in CI, consistent with repository narrow-scope test rules.
- Later add scale/performance tests after persistent V2 index integration.

## Risks tracked

- Linux filesystem case sensitivity differs from Windows; user-facing search remains case-insensitive while paths preserve original case.
- Symlink recursion can create loops; initial scanner does not recurse into symbolic-link directories.
- Permission failures must not abort a whole scan.
- `inotify` limits and event overflow are deferred until the persistent/incremental indexing step.
