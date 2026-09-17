# Step 0009 — Linux search query filters

Date: 2026-09-17
Branch: `linux-port`

## Goal

Advance Stage 7 search feature parity without destabilizing the now-green daemon/UI foundation.

## Decisions

- Add a small Linux-native query parser rather than linking the Windows-oriented upstream query stack prematurely.
- Preserve upstream fzf ranking for free-text terms.
- Apply structural filters before fuzzy matching to reduce work.
- Support a deliberately small, composable syntax first: `ext:<extension>`, `type:file|dir`, and `path:<substring>`.
- Allow filter-only queries. This is important for workflows such as `ext:pdf type:file` where no fuzzy name term is needed.

## Implementation

- Added `LinuxSearchQuery` and `LinuxEntryKind`.
- Updated `LinuxFuzzySearch.Search` to parse filters, reject non-matching entries, and then use the existing linked upstream fzf matcher for remaining text.
- Added MSTest coverage for parsing, extension/type filters, path + fuzzy matching, filter-only queries, and invalid type values.

## Validation

Validation is delegated to the existing Ubuntu CI quality gate, which builds Linux Core/CLI/Daemon/App in Release, runs Linux MSTest, and executes smoke/integration checks. The changes are intentionally isolated to `Linux/Core` and `Linux/Tests`.

## Remaining Stage 7 work

- query quoting/escaping and richer predicates;
- history/recent results;
- bookmarks/favorites;
- application launcher source;
- cross-platform plugin boundary;
- representative large-index performance acceptance.
