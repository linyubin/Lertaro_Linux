# 0013 — Application daemon protocol

Date: 2026-09-18
Branch: `linux-port`

## Goal

Continue Stage 7 by exposing the freedesktop application catalog through the existing per-user daemon protocol so CLI and Avalonia clients can consume one authoritative application source.

## Decision

Add a dedicated `application-list` request instead of mixing application entries into filesystem search results. File results and desktop applications have different activation semantics and metadata; keeping the protocol types separate avoids overloading path fields and makes later ranking changes explicit.

The existing request `Query` and `Limit` fields are reused. An empty query lists applications in catalog order; a non-empty query applies case-insensitive name filtering. Limits remain bounded to 1–200 to keep a local client from generating unnecessarily large responses.

## Implementation

- Added `LinuxDaemonApplicationItem` and the optional `Applications` response payload.
- Added the `application-list` daemon command.
- Application discovery continues to use `LinuxApplicationCatalog`, preserving XDG priority, masking and visibility behavior.
- Added a daemon-protocol round-trip test for application payload metadata.

## Validation

`Linux/Tests/Linux.Tests.csproj` remains the narrow automated target. Each push triggers `Linux Port CI` on Ubuntu; the new protocol test plus the existing application-catalog tests are the acceptance gate for this step.

## Next

Consume `application-list` in the Avalonia result model and add safe desktop-ID activation through the desktop environment. Then continue Stage 7 with performance acceptance instrumentation and plugin-boundary review.
