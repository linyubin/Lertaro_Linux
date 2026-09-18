# 0022 — Linux plugin compatibility boundary

Date: 2026-09-18
Branch: `linux-port`

## Goal

Close the final Stage 7 feature-parity decision without introducing an unsafe last-minute compatibility layer for the Windows plugin ecosystem.

## Decision

The initial Ubuntu release does not claim binary compatibility with upstream Windows or Flow Launcher plugins. Windows-specific plugin DLLs are not loaded into the Linux daemon or Avalonia process.

The rationale and future direction are documented in `Linux/PLUGIN_COMPATIBILITY.md`. A future Linux extension API should be versioned, OS-neutral and preferably hosted out of process so plugin crashes, hangs and incompatible dependencies cannot destabilize indexing/search.

## Implementation

- added `Linux/PLUGIN_COMPATIBILITY.md` with supported and unsupported boundaries;
- explicitly documented that filesystem search, filters, history, bookmarks, application launch, daemon IPC and freedesktop actions are supported product capabilities rather than a third-party binary plugin compatibility promise;
- marked the plugin-boundary release gate complete;
- reconciled Stage 7 in `DevelopmentLogs/ROADMAP.md` to `completed`;
- updated Stage 8 summary to reflect restart/corruption and performance gates already landed.

## Validation

Documentation-only milestone; no production code changed, so AGENTS.md does not require MSTest or a narrow project build. The branch CI remains the release regression authority and the documentation commits trigger Linux Port CI.

## Next

Stage 8 is now the only incomplete roadmap stage. Finish user-facing Ubuntu install/start/uninstall and known-limitations documentation, convert already-passing performance/recovery evidence into final checklist sign-off where appropriate, and execute the formal release candidate matrix. Manual GNOME/Wayland and KDE/Plasma desktop checks must be recorded honestly rather than inferred from Xvfb.