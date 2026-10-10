# Copilot Instructions

## Project Guidelines
- Prefer ore map tiles to use point-top geometry and the overall ore map shape to match the game's hex grid (mask union of point-top hexes).
- Use `HexMath.GenerateCellsForCountSorted` helper as the canonical axial cell list generator; prefer a single source of truth for hex layout so UI, ore generator, and grid use the same axial ordering and radius.
- Use `MinableComponent.worldRadius` and `hexSize` as authoritative geometry when `useMinableComponentHexGridSize` is enabled; ore generator and UI should read blocked tiles from `MinableComponent.tileData.isBlocked`.
- Annotate code examples with explicit comment markers showing added/changed lines and include those markers in design docs so developers can quickly find changes.