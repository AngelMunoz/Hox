# Changelog

## [Unreleased]

### Added

- **Hox:** net10.0 is now a supported target for `Hox` and `Hox.Feliz`.

### Changed

- **Hox:** CSS selector parsing now uses XParsec instead of FParsec. Selector errors throw with a new, more detailed error format.
- **Hox:** Duplicate ids in a selector now keep the first value instead of crashing, duplicate attributes are kept in order, and attributes render in the order they were written.
- **Hox:** Updated FSharp.Control.TaskSeq to 1.1.1.

### Fixed

- **Project:** Source builds work again on current .NET 10 SDK releases, whose F# compiler rejects public inline functions that reference private bindings.

## [3.1.0] - 2026-01-03

### Fixed

- **Hox:** Attribute handling and rendering logic refactored for correctness.

## [3.0.0] - 2026-01-02

### Changed

- **Hox (breaking):** Rendering rewritten to remove recursion and adopt the new `Node`/element model. Update node construction and render calls to the new API.

[Unreleased]: https://github.com/AngelMunoz/Hox/compare/v3.1.0...HEAD
[3.1.0]: https://github.com/AngelMunoz/Hox/compare/v3.0.0...v3.1.0
[3.0.0]: https://github.com/AngelMunoz/Hox/releases/tag/v3.0.0
