# Step 0008 — CI stabilization for Avalonia UI and Linux tests

Date: 2026-09-17
Branch: `linux-port`

## Goal

Restore the Ubuntu quality gate after the initial desktop UI landed, and treat CI failures as blocking defects before advancing feature work.

## Failures observed

1. Avalonia generated a partial `MainWindow` class and named controls, while the code-behind declared a non-partial class and duplicate `FindControl` properties.
2. After resolving that, compiled XAML could not infer the result item type for `Name` and `Path` bindings, and `Watermark` produced an obsolete API warning.
3. Once the desktop app built, the Linux test project exposed a reversed MSTest `Assert.Contains` call plus analyzer warnings for timeout cancellation, collection assertions, and test parallelization configuration.

## Decisions

- Keep Avalonia compiled bindings enabled and provide an explicit `x:DataType` rather than disabling binding validation.
- Use generated named controls instead of duplicating control lookup properties.
- Keep analyzer output clean instead of suppressing warnings, matching `AGENTS.md`.
- Disable test parallelization for the Linux integration suite because multiple tests exercise filesystem watchers, Unix sockets, and temporary filesystem state where deterministic sequencing is preferable.

## Implemented

- Made `MainWindow` partial and removed duplicate named-control properties.
- Added `x:DataType="core:LinuxDaemonSearchItem"` to the result template.
- Replaced obsolete `Watermark` with `PlaceholderText`.
- Corrected the `Assert.Contains` argument order in desktop-action tests.
- Enabled cooperative cancellation on timeout-bound daemon and watcher tests.
- Replaced a raw count equality assertion with `Assert.HasCount`.
- Added assembly-level `DoNotParallelize` for the Linux MSTest project.

## Validation

Ubuntu CI is automatically rerun after each commit. The prior UI compile blockers have been removed; the next run is expected to execute the complete build, unit/integration test, direct-scan, persistent-index, and daemon/watcher smoke-test sequence. Any remaining failure is considered blocking and will be fixed before feature-parity work continues.

## Next step

After CI is green, continue Stage 7 search feature parity, then Stage 8 packaging and release acceptance.
