# Step 0002 — Linux MVP bootstrap

Date: 2026-09-17
Branch: `linux-port`

## Implemented

- Added `Linux/Core` targeting platform-neutral `net10.0`.
- Reused upstream low-level fzf implementation by linking source files from `Core/SearchIndex/Fzf`; no forked copy of the algorithm was created.
- Added a managed recursive filesystem scanner that preserves Linux paths, tolerates inaccessible entries, reports file size and avoids recursing through symlink/reparse-point directories.
- Added `Linux/Cli`, providing the first end-to-end vertical slice: scan a root, fuzzy-search filenames and print ranked paths plus scan/search timing.
- Added MSTest coverage for recursive scanning, dot files, missing roots, fuzzy subsequence matching, case-insensitive matching and argument validation.
- Added an Ubuntu GitHub Actions workflow that builds only the Linux CLI, runs only the Linux test project and executes a CLI smoke test.

## Current architecture

`Linux/Cli -> Linux/Core -> linked upstream Fzf sources`

The scanner currently materializes the scanned entries in the CLI before searching. This is intentional for the first vertical slice; it proves Linux filesystem access and upstream fuzzy matching independently of the Windows-only project graph.

## Deliberate limitations

- No persistent snapshot/index yet.
- No inotify incremental updates yet.
- Search currently matches filenames, not full Lertaro query syntax/path tokens.
- No GUI or global hotkey yet.

## Automated validation

GitHub Actions workflow: `.github/workflows/linux-ci.yml`.

Expected checks:

1. `dotnet build Linux/Cli/Linux.Cli.csproj --configuration Release`
2. `dotnet test Linux/Tests/Linux.Tests.csproj --configuration Release`
3. CLI smoke test against the repository's `Linux` directory.

## Next step

After CI is green, introduce a persistent Linux index boundary and integrate the reusable V2 snapshot/search components incrementally. If CI exposes compile/runtime incompatibilities in the linked fzf subset, fix those first and record them in the next development log.
