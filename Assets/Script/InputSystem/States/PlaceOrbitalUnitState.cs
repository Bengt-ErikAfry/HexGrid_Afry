using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using static SaveManager;


#if UNITY_EDITOR
using UnityEditor;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
#endif

public class PlaceOrbitalUnitState : IGameState
{
    private readonly GameStateMachine _fsm;

    public PlaceOrbitalUnitState(GameStateMachine fsm) => _fsm = fsm;

    // Enter State.
    public void Enter()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.activeState.text = "Active state: PlaceOrbitalUnitState State";
        }

        //Hide buttons
        UIManager.Instance.HideAllUI();
    }
    // Exit State.
    public void Exit() { }

    public void OnTap(Vector2 screenPos)
    {
        //Reset collections and highlights when player taps on the screen, so that player can select new unit or asteroid.
        SelectionService.Instance.ClearSelection();
        TileManager.Instance.ClearHighlightedTiles();
        UIManager.Instance.HideStackView();

        Debug.Log("OnTap called.");
        if (CameraManager.Instance.isMovingCamera) return;

        var cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(screenPos);

        // 1) Get physics hits along ray (3D colliders) — keep for debug/fallback
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // Send ray to a plane at z=0 to get the world position of the tap.
        Plane plane = new Plane(Vector3.forward, Vector3.zero);
        if (!plane.Raycast(ray, out float enter))
        {
            Debug.LogWarning("Tap ray missed the gameplay plane.");
            return;
        }
        Vector3 worldPos = ray.GetPoint(enter);

        Debug.Log($"Tap at screenPos {screenPos}, worldPos {worldPos}, hits={hits.Length}");

        // Get the hex tile that was clicked on.
        HexGrid_TilePrefab hexClicked = TileManager.Instance.GetTileFromWorldPosition(worldPos);
        if (hexClicked == null)
        {
            // 1) player clicked outside tilegrid

            Debug.Log("SelectState OnTap: clicked outside tile grid -> cleared selection/highlights.");
            return;
        }

        // Compute authoritative axial coordinate for the clicked world position.
        Vector2Int clickedCoord = TileManager.Instance.WorldToAxial(worldPos);
        Debug.Log($"SelectState OnTap at TILE coord {clickedCoord} and WorldPos {worldPos}");

        // Find units located on the same hex (use GameManager authoritative list).
        var unitsInClickedHex = new List<Unit>();
        if (GameManager.Instance != null && GameManager.Instance.unitsList != null)
        {
            unitsInClickedHex = GameManager.Instance.unitsList
                .Where(u => u != null && TileManager.Instance.WorldToAxial(u.transform.position) == clickedCoord)
                .Distinct()
                .ToList();
        }

        // Case 2: empty tile (no asteroid and no unit)
        if (unitsInClickedHex.Count == 0)
        {
            SelectionService.Instance.SetSelectedHex(worldPos);
            TileManager.Instance.HighlightTileWorldPosition(worldPos);
            Debug.Log("SelectState OnTap: empty tile -> selected hex and highlighted.");

            if (AsteroidManager.Instance.unitScriptToPlace.unitType == UnitType.Satelite)
            {
                //Place a satelite
                Debug.Log("Place Satelite.");
                AsteroidManager.Instance.PlaceSateliteOnSurface(clickedCoord);
            }
            else
            {
                //Other unit.
                Debug.Log("Place Other.");
                //SaveManager.Instance.LoadSavedUnitById(AsteroidManager.Instance.unitScriptToPlace, GameManager.Instance.gameViewUnitsSaveFile, clickedCoord);
            }

            //Not used anymore when unit in orbit are no longer destroyed when asteroidview is loaded. Use unit script insted.
            /*
            //Get unit SaveData to read unittype and range.
            SaveManager.UnitSaveData unitSaveData = SaveManager.Instance.GetUnitSaveDataById(AsteroidManager.Instance.unitToPlaceUnitId, GameManager.Instance.gameViewUnitsSaveFile);

            if(unitSaveData == null)
            {
                Debug.Log("PlaceOrbitalUnitState: saveData for current unit missing.");
                return;
            }

            UnitType unitToPlaceUnitType = unitSaveData.unitType;

            if(unitToPlaceUnitType == null)
            {
                Debug.LogWarning("PlaceOrbitalUnitState: UnitType missing.");
                return;
            }

            // The saved moduleRuntimeList is not a list only saves strings. Need convert it to a list to be abel to look thrue it.
            //var savedModules = unitSaveData.moduleRuntimeList ?? new List<ItemInstanceSaveData>();
            var runtimeModules = new List<ItemInstance>();

            foreach (var saved in unitSaveData.moduleRuntimeList)
            {
                if (saved == null)
                {
                    runtimeModules.Add(new ItemInstance
                    {
                        ItemDefinition = null,
                        isOnline = false,
                        isBroken = false,
                        isDestroyd = false
                    });
                    continue;
                }

                var def = SaveManager.Instance.ResolveSavedItemDefinition(saved.itemId);

                runtimeModules.Add(new ItemInstance
                {
                    ItemDefinition = def,
                    currentPowerUpBoost = saved.currentPowerUpBoost,
                    isOnline = saved.isOnline,
                    isBroken = saved.isBroken,
                    isDestroyd = saved.isDestroyed
                });
            }

            if (unitToPlaceUnitType == UnitType.Satelite)
            {
                //Place a satelite
                Debug.Log("Place Satelite.");
                AsteroidManager.Instance.PlaceSateliteOnSurface(clickedCoord, runtimeModules);
            }
            else
            {
                //Other unit.
                Debug.Log("Place Other.");
                SaveManager.Instance.LoadSavedUnitById(AsteroidManager.Instance.unitToPlaceUnitId, GameManager.Instance.gameViewUnitsSaveFile, clickedCoord);
            }
            */
            return;
        }

        // Case 5 or multiple units: tile with multiple units OR asteroid + one or more units -> show stack
        if (unitsInClickedHex.Count > 1 || (unitsInClickedHex.Count >= 1))
        {
            UIManager.Instance.ShowStackView(unitsInClickedHex,null, screenPos);
            TileManager.Instance.HighlightTileWorldPosition(worldPos);
            Debug.Log("SelectState OnTap: multiple units or units+asteroid -> showing stack view.");
            return;
        }

        // Case 3: single unit (no asteroid)
        if (unitsInClickedHex.Count == 1)
        {
            var unit = unitsInClickedHex[0];
            SelectionService.Instance.SetSelectedUnit(unit);

            // Show Route if existing
            if (unit.routeComponent != null && unit.routeComponent.routeActions.Count > 0)
            {
              //  HexGridManager.Instance.CalculateRoutePath(unit);
            }

            TileManager.Instance.HighlightTileWorldPosition(worldPos);
            Debug.Log($"SelectState OnTap: single unit '{unit.unitName}' selected.");
            return;
        }

        // Fallback: highlight tile
        TileManager.Instance.HighlightTileWorldPosition(worldPos);
    }

    private static ItemDefinition ResolveSavedItemDefinition(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        if (ItemDatabase.Instance != null)
        {
            var dbDef = ItemDatabase.Instance.GetById(itemId);
            if (dbDef != null)
                return dbDef;
        }

        // Important: do NOT restrict to a single folder like "ItemDefinitions".
        // Your modules live under Resources/Modules, Resources/Ships, etc.
        var allDefs = Resources.LoadAll<ItemDefinition>("");
        foreach (var def in allDefs)
        {
            if (def != null && def.itemId == itemId)
                return def;
        }

        return null;
    }

    public void OnDrag(Vector2 delta)
    {
        //Return if player is interacting with UI, so camera does not move when player is interacting with UI
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        if (UIManager.Instance.stackViewRectTransform.gameObject.activeSelf == true) return;
        var move = new Vector3(-delta.x * 0.01f, -delta.y * 0.01f, 0);
        Camera.main.transform.Translate(move, Space.World);
    }

    public void OnPinch(float amount)
    {
        Camera.main.orthographicSize = Mathf.Clamp(
            Camera.main.orthographicSize - amount,
            CameraManager.Instance.minZoom,
            CameraManager.Instance.maxZoom);
    }

    public void Tick(float dt) { }
}
