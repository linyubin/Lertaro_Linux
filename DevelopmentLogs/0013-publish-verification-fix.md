# 0013 — Linux publish verification fix

Date: 2026-09-18
Branch: `linux-port`

## Trigger

The bookmark CLI change passed the `linux-core` Ubuntu job, including builds, MSTest, direct search, persistent-index smoke testing, daemon watcher testing, and the new bookmark add/list/remove round trip. The workflow still failed in the self-contained publish matrix.

## Root cause

`dotnet publish` succeeded for the Linux projects, but the verification step looked for executables named `Lertaro.Linux.<Project>`. The projects deliberately set Linux-friendly assembly names instead:

- CLI: `lertaro-linux`
- daemon: `lertarod`
- desktop app: `lertaro-ui`

The failure was therefore in CI acceptance logic, not compilation or publishing.

## Fix

Changed the publish verification step to map each project to its declared executable name and assert that the resulting native launcher exists and is executable for both `linux-x64` and `linux-arm64`.

## Validation

A fresh `Linux Port CI` run is triggered by the workflow update. Acceptance requires both the `linux-core` job and all six self-contained publish matrix jobs to pass.

## Next

Resume Stage 7 only after this CI regression is green. Then expose bookmarks in the Avalonia interaction surface and add representative performance acceptance checks before Stage 8 packaging.
