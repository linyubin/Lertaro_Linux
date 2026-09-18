# Lertaro Linux plugin compatibility boundary

Date: 2026-09-18

## Release decision

The first supported Ubuntu release does **not** load the upstream Windows plugin stack in-process and does not claim Flow Launcher plugin compatibility. The Linux port treats plugins as a future extension surface rather than a release-critical compatibility promise.

This boundary is intentional. The upstream application is Windows-oriented and its extension ecosystem can depend on WPF, Win32, NT services, registry state, Windows shell behavior, Windows executable conventions, and other APIs that do not have a reliable Linux equivalent. Loading such assemblies into the Linux daemon or Avalonia process would turn optional extensions into a stability and security risk.

## Supported cross-platform subset in the first release

The supported extension surface is the functionality implemented directly by the Linux projects and exposed through their existing process boundaries:

- filesystem search and path-aware filters;
- persistent history and bookmarks;
- Linux `.desktop` application discovery and launch;
- daemon requests over the Unix-domain-socket protocol;
- freedesktop-compatible file open/reveal behavior.

These are product features, not a binary compatibility guarantee for third-party plugin assemblies.

## Explicitly unsupported for the first release

- loading arbitrary upstream Windows plugin DLLs;
- WPF-dependent plugin UI;
- Win32/COM/registry/NT-service plugin integrations;
- Windows shell-extension plugins;
- Flow Launcher plugin compatibility;
- plugins that execute inside the indexing daemon process;
- claiming source or binary compatibility with the Windows plugin SDK.

Unsupported plugins must not be silently loaded. Future plugin work must preserve daemon/index stability when an extension crashes, hangs, or is incompatible.

## Future architecture direction

If Linux plugin support is added, prefer a small versioned, OS-neutral contract and an out-of-process host. Candidate capabilities are query providers, result actions, aliases and optional metadata/preview providers. The contract should use serializable DTOs rather than UI framework types, Windows handles, registry types or service objects.

An out-of-process host is preferred because it provides a practical isolation boundary for timeouts, crashes, resource limits and incompatible dependencies. Cross-platform providers should declare capabilities explicitly; OS-specific providers should declare their supported platforms and fail closed when unavailable.

## Acceptance consequence

Plugin parity is therefore defined as a documented compatibility boundary for the initial Ubuntu release, not as porting every Windows plugin. This decision closes the Stage 7 plugin-boundary requirement without adding an unstable compatibility layer immediately before release. A later plugin milestone should have its own API versioning, isolation, packaging and compatibility tests.