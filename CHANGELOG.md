# Changelog

All notable changes to this project are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.3.0] - unreleased

### Added
- Output-formatting options on `StringTemplateConfiguration` (all off by default): `FormatProvider` applies an `IFormatProvider` / culture to format specifiers; `HtmlEncode` HTML-encodes substituted values (not the template literals); `TrimBlockWhitespace` trims whitespace and a trailing newline around block tags so control tags on their own line do not leave blank lines. Each has a fluent setter and is carried through `ITemplateRenderer`.

## [2.2.0] - 2026-07-29

### Added
- `{else if}` chains and negation (`!`) in conditions: `{if A}...{else if B}...{else}...{/if A}` renders the first matching branch, and an `{else if ...}` can test any property. Prefix any condition with `!` to negate it, for example `{if !IsActive}`.
- Inline null-coalesce fallback: `{Prop ?? "N/A"}` renders the value, or the (optionally quoted) fallback when the property is null or missing.

### Changed
- Property accessors are compiled (expression trees) and cached per type, so repeated renders of the same type skip the reflection walk.
- Rendering memory stays bounded on long-running and free-form (`ExpandoObject`) workloads: per-key work is skipped for keys the template does not reference, so the internal regex cache is bounded by the template's tokens rather than the data's property names, and that cache is additionally hard-capped.

### Fixed
- A null reference-type property no longer throws while flattening (the flattener no longer recurses into null).
- Deeply nested dot notation such as `{A.B.C}` now flattens to the correct key.

## [2.1.0] - 2026-07-28

### Added
- Loop metadata inside `foreach`: `{index}` (0-based), `{first}`, `{last}`, and `{count}`. Since `{first}` and `{last}` are booleans, they compose with `{if}` for things like separators. If an item already has a property with one of those names, the item's value is used.
- Benchmark projects (BenchmarkDotNet): a suite you can run against your own template and data, plus a cross-version comparison whose report is generated into the README.

### Fixed
- Inherited properties are now rendered. Previously only properties declared on the exact type were reflected, so anything inherited from a base class was silently skipped.
- Format specifiers now work with custom tokens that contain regex metacharacters (like `[[` / `]]`) and with dotted keys (`{Product.Price:0.00}`). These were not being escaped in the format matcher.

### Changed
- Regex instances are built once and reused across renders instead of being rebuilt on every call.

## [2.0.0] - 2026-07-27

Major modernization. **Breaking:** the library now targets `netstandard2.0`, so the down-level `netstandard1.0` / `net45` builds are no longer produced. The 1.x line stays supported for projects on older frameworks.

### Added
- Dependency injection: an `ITemplateRenderer` / `TemplateRenderer` instance API and the `IStringTemplateConfiguration` abstraction, with no dependency on any DI container. `StringTemplateConfiguration.Create(...)` builds a configuration via the fluent interface.
- Extended `{if}`: comparison operators (`==`, `!=`, `>`, `<`, `>=`, `<=`), an `{else}` block, enum matching by name (case-insensitive) or number, and property-to-property comparison with `@` (for example `{if Total >= @Minimum}`).
- Validated support for custom `IEnumerable` implementations in `foreach`, with tests.
- Configurable `{else}` token via `StringTemplateConfiguration.ElseToken`.
- Package now ships SourceLink, a symbol package (`.snupkg`), and an embedded README.

### Changed
- Retargeted the library to `netstandard2.0`; tests and samples target `net8.0`.
- The static `StringTemplate` API is unchanged, so existing templates render the same.

[2.3.0]: https://github.com/kylepmanuel/nlighttemplate/compare/v2.2.0...v2.3.0
[2.2.0]: https://github.com/kylepmanuel/nlighttemplate/compare/v2.1.0...v2.2.0
[2.1.0]: https://github.com/kylepmanuel/nlighttemplate/compare/v2.0.0...v2.1.0
[2.0.0]: https://github.com/kylepmanuel/nlighttemplate/compare/v1.1.0...v2.0.0
