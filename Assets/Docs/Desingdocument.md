\# Space Hex Strategy Game

Is a super simpel turn based strategy game for mobile phones.

Focus on simple code and easy to understand for developer.

Chose simpel but maybe more performance heavy solution before a high performant solution that is hard to understand.

 

\## Architecture

 

Single Gameplay Scene

 

Views:

\- Galaxy View

\- Asteroid View

\- Interior View

 

\## Persistence

 

Template JSON

\- Ship layouts

\- Asteroid layouts

\- Galaxy layouts

 

Instance JSON

\- Fog of war

\- Unit positions

\- Door states

\- Ore remaining

\- Enemy states

 

\## Turn System

 

Strategic Turns:

\- Galaxy

\- Asteroids

\- Mining

\- Fleet movement

 

Combat Turns:

\- Boarding

\- Outpost combat

 

Combat turns do not advance strategic turns.

 

\## Hex Editor

 

Editor tool that:

\- Displays ghost hex grid

\- Click places tile prefab

\- Shift-click removes tile

\- Exports JSON templates2



\## Hex grid

The hex grid are only used for editor placement of tiles.

In game i use tiles for highlighting, navigation.

Do not use HexHighlighter.cs.

Do not use HexGridManager.cs.

Use tilemanager.cs to highlight tiles. Tiles are now Gameobjects witha child gameobject that are setactiv true to highlight and setactive false to deactivate.



In tilemanager.cs do not remove HighlightTileWorldPosition() and ClearHighlightedTiles().



Always mark new code with "//New" and "//End new" in code examples when adding new code.

Always mark code with "//Add this" to "//Stop add" and "//Remove this" to "//Stop remove" in the code example when changing out code.





\- Editorexport/import notes

\- HexLayoutEditor should write `prefabPath` (for human-editing) and `visibility`.

\- At runtimeprefer `prefabName` or a registry key in JSON if using Resources/Addressables.



\- Migration / compatibility

\- Keep `HexGridManager` as shim during migration; prefer calling `TileManager` for new runtime code.

\- Add clearcomment blocksin modified files to showwhat was added during migration (see example below).

\## Code changes \& migration notes (ADD HERE)



\- Runtime vs Editor

&#x20; - Editor tools (HexLayoutEditor) may use Editor-only APIs (`PrefabUtility`, `AssetDatabase`) and write JSON with `prefabPath`.

&#x20; - Runtime loaders MUST resolve prefabs via one of:

&#x20;   - `prefabRegistry` (inspector mapping key -> prefab)

&#x20;   - `Resources.Load` (prefabs in `Resources/` using filename)

&#x20;   - Addressables (preferred for production)

&#x20; - Do NOT call `AssetDatabase` in builds.



\- Tile conventions (canonical)

&#x20; - Tile coordinate convention: `coord.x = tileIndexCol`, `coord.y = tileIndexRow`.

&#x20; - Use `HexMath` for axial<->world conversions:

&#x20;   - `HexMath.AxialToWorldCenter\_PointTop(a, size)`

&#x20;   - `HexMath.WorldToAxial\_PointTop(world, size)`

&#x20; - Tile prefab must include `HexGrid\_TilePrefab` with:

&#x20;   - `public int tileIndexCol;`

&#x20;   - `public int tileIndexRow;`

&#x20;   - `public Image tileHighlightImage;`

&#x20;   - `public bool visibility;` (optional)

&#x20;   - `public bool\[] walls = new bool\[6];` (optional, point-top order)



\- TileManager responsibilities

&#x20; - Build tile lookup (`BuildTileLookup()`) from `tilesParent`.

&#x20; - Provide: `GetTileFromWorldPosition`, `WorldToAxial`, `AxialToWorldCenter`, `GetReachableTiles`, `FindPath`, `CanPass`, `HighlightCoords`, `ClearHighlights`, `LoadLayoutFromJsonText`.

&#x20; - `LoadLayoutFromJsonText` must map JSON `prefabPath` -> runtime prefab using the runtime mapping strategy above.



\- JSON schema (example)

