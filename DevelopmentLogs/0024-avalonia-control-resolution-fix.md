# 0024 — Avalonia control-resolution startup fix

Date: 2026-09-18
Branch: `linux-port`

## Goal

Restore the graphical UI release gate after Linux Port CI run #52 exposed a real startup regression in `MainWindow`.

## Diagnosis

Run #52 built all Linux projects and passed all 36 MSTest tests, but the Xvfb process-level UI smoke gate crashed with a `NullReferenceException` in `MainWindow..ctor()` while assigning the results collection. The failure occurred after `AvaloniaXamlLoader.Load(this)`, showing that relying directly on generated named-control members was not robust in this startup path.

This is treated as an application defect rather than weakening or removing the graphical acceptance gate.

## Implementation

Updated `Linux/App/MainWindow.axaml.cs` to resolve `QueryBox`, `ResultsList`, and `StatusText` explicitly with `FindControl<T>` immediately after XAML loading. The resolved controls are stored in private readonly fields and all subsequent UI logic uses those fields.

Each lookup now fails with a specific `InvalidOperationException` if the XAML/control contract is broken, replacing an ambiguous null dereference with an actionable startup error.

No search, daemon protocol, activation, or application-launch behavior was changed.

## Validation

The production-code commit triggers `Linux Port CI`. Acceptance requires:

- Release builds for CLI, daemon, and Avalonia app;
- all targeted Linux MSTest tests;
- the Xvfb graphical startup gate to remain alive for its five-second observation window;
- downstream direct-scan, persistence, daemon/recovery, performance, publish, and Debian package gates to remain green.

## Next

If the matrix is green, continue closing the remaining release checklist rather than declaring completion: manual GNOME/Wayland evidence, KDE/Plasma evidence where practical, and the formal release-candidate matrix/sign-off remain release gates.
