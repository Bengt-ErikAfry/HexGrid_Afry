using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Assemblies;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;


public class AsteroidManager : MonoBehaviour
{
    public AsteroidComponent currentAsteroidComponent;
    public MinableComponent currentMinableComponent;
    public Transform uintInOrbitView;
    public Transform unitInOrbitListContent;
    public GameObject unitInOrbitPrefab;
    public GameObject unitInOrbitInventoryUnitPrefab;
    public Unit unitScriptToPlace;  // Use this insted of unitID.
    public SpriteRenderer asteroidBackgroundRenderer; // Will be loaded when asteroid is loaded and changed when player click the display ore map button..
    public bool isOreMapDisplayed = false; // Track whether the ore map is currently displayed or not.
    public Unit itemSourceShip; //Used when player place a building on the surface to knowwhat inventory to delete items from.
    public static AsteroidManager Instance { get; private set; }

    public List<Unit> unitsInOrbit = new List<Unit>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ShowAsteroidView(AsteroidComponent asteroid)
    {
        if (asteroid == null)
        {
            Debug.LogWarning("ShowAsteroidView: asteroid is null.");
            return;
        }

        if (string.IsNullOrWhiteSpace(asteroid.asteroidTileSatusSave))
        {
            Debug.LogError("ShowAsteroidView: asteroidJsonFileName is null or empty.");
            return;
        }

        // 1) Clear the current view / path / UI
        TileManager.Instance?.ClearHighlights();
        TileManager.Instance?.ClearPath();
        UIManager.Instance?.HideAllUI();
        SelectionService.Instance?.ClearSelection();
        SelectionService.Instance?.ClearSelectedAsteroid();

        // 2) Save current fog state before replacing the layout
        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.gameViewLayout))
        {
            TileManager.Instance?.SaveFogState(GameManager.Instance.gameViewLayout);
        }

        // 3) save all units in orbit.
        unitsInOrbit = TileManager.Instance?.GetUnitsOnTile(asteroid.currentTileCoord) ?? new List<Unit>();

        // 3.1) set current asteroid
        currentAsteroidComponent = asteroid;

        // 3.5) SaveAllUnits and SaveAllInventories to ensure the current state is preserved before switching views
        SaveManager.Instance?.SaveAllUnits(GameManager.Instance.gameViewUnitsSaveFile, asteroid);
        SaveManager.Instance?.SaveAllInventories();

        // 3.6) Save all Asteroid data
        SaveManager.Instance?.SaveAllAsteroids(GameManager.Instance.gameViewAsteroidsSaveFile);

        // 4) Hide all active game-view units and asteroids
        SaveManager.Instance?.HideGameViewObjects(asteroid);

        // 5) Load the asteroid interior layout
        string path = Path.Combine(Application.streamingAssetsPath, asteroid.asteroidTileSatusSave);
        TileManager.Instance?.LoadLayoutFromFile(path);

        // 6) Load asteroid-surface units
        SaveManager.Instance?.LoadAsteroidViewObject(asteroid);

        // 7) Register only the units that are actually active in this view
        //    This prevents hidden game-view units from still contributing to fog/visibility.
        if (GameManager.Instance != null)
        {
            foreach (var unit in GameManager.Instance.playerUnits)
            {
                if (unit == null) continue;
                if (!unit.gameObject.activeSelf) continue;

                int range = Mathf.CeilToInt(unit.detectionRange);
                TileManager.Instance?.RegisterUnit(unit, range);
            }
        }

        // 8) Remember the asteroid for restoring later
        //SaveCurrentAsteroid(asteroid);

        // 9) Set state
        GameStateMachine.Instance?.SetState(GameplayStateId.Selecting);

        // 10) populate the orbital units UI
        uintInOrbitView.transform.gameObject.SetActive(true);
        UpdateUnitInOrbitView();

        Debug.Log($"Entered asteroid view: {asteroid.asteroidName}");
    }


    public void HideAsteroidView()
    {
        // 1) Clear path and selection visuals
        TileManager.Instance?.ClearHighlights();
        TileManager.Instance?.ClearPath();
        SelectionService.Instance?.ClearSelection();
        SelectionService.Instance?.ClearSelectedAsteroid();

        // Cleare and hide the orbital units UI
        uintInOrbitView.transform.gameObject.SetActive(false);
        unitsInOrbit.Clear();

        // 2) Hide UI
        UIManager.Instance?.HideAllUI();

        // 3) Save asteroid fog state before leaving
        if (currentAsteroidComponent != null)
        {
            TileManager.Instance?.SaveFogState(currentAsteroidComponent.asteroidTileSatusSave);
        }

        // 3.5 ) SaveAllUnits and SaveAllInventories to ensure the current state is preserved before switching views
        SaveManager.Instance?.SaveAllUnits(currentAsteroidComponent.unitsOnSurfaceSave);
        SaveManager.Instance?.SaveAllInventories();

        // 4) Hide all active game-view units and asteroids
        SaveManager.Instance?.HideAsteroidViewObjects();

        // 4) Restore the original game-view layout
        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.gameViewLayout))
        {
            string path = Path.Combine(Application.streamingAssetsPath, GameManager.Instance.gameViewLayout);
            TileManager.Instance?.LoadLayoutFromFile(path);
        }

        // 5) Restore fog state for the game view
        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.gameViewLayout))
        {
            TileManager.Instance?.LoadFogState(GameManager.Instance.gameViewLayout);
        }

        // 6) Re-show the original game-view objects
        SaveManager.Instance?.LoadAllUnits(GameManager.Instance.gameViewUnitsSaveFile);

        // 7) Re-register only active player units that are back in the game view
        if (GameManager.Instance != null)
        {
            foreach (var unit in GameManager.Instance.playerUnits)
            {
                if (unit == null) continue;
                if (!unit.gameObject.activeSelf) continue;

                int range = Mathf.CeilToInt(unit.detectionRange);
                TileManager.Instance?.RegisterUnit(unit, range);
            }
        }

        // 7.5) Load all Asteroid data back into the game view
        SaveManager.Instance?.LoadAllAsteroids(GameManager.Instance.gameViewAsteroidsSaveFile);

        // 8) Set normal state
        GameStateMachine.Instance?.SetState(GameplayStateId.Selecting);

        // 8.5 Move units in orbit back to gameview
        foreach (var unit in AsteroidManager.Instance.unitsInOrbit)
        {
            unit.transform.SetParent(GameManager.Instance.unitInGameview, false);
            unit.transform.localPosition = TileManager.Instance.AxialToWorldCenter(unit.currentTileCoord);
            unit.transform.GetChild(0).gameObject.SetActive(true);
        }

        // 9) Clean up the temporary asteroid GameObject
        currentAsteroidComponent = null;
        currentMinableComponent = null;

        Debug.Log("Returned from asteroid view.");
    }
    public void UpdateUnitInOrbitView()
    {
        //Clear Lsit
        foreach (Transform child in unitInOrbitListContent)
        {
            Destroy(child.gameObject);
        }

        //Populate the list with units that are currently in orbit
        foreach (var unit in unitsInOrbit.Where(u => u != null && u.gameObject != null))
        {
            var unitInOrbitListEnery = Instantiate(unitInOrbitPrefab, unitInOrbitListContent);
            OrbitingUnitPrefab_Entery entry = unitInOrbitListEnery.GetComponent<OrbitingUnitPrefab_Entery>();
            if (entry != null)
            {
                entry.SetUnit(unit);
            }
            else
            {
                Debug.LogError("OrbitingUnitPrefab_Entery component not found on the prefab.");
            }
        }
    }

    public void PlaceSateliteOnSurface(Vector2Int coordClicked)
    {
        //Reveal tiles around the satelite based on its sensor range
        int sensorRange = ModuleManager.Instance.GetSensorRange(unitScriptToPlace.moduleRuntimeList);
        Debug.Log($"PlaceSateliteOnSurface: SensorRange = {sensorRange}");
        TileManager.Instance.RevealTilesAroundAxial(coordClicked, sensorRange);

        //Remove satelite from hiracy
        foreach (Transform unitTransform in GameManager.Instance.UnitsInOrbit)
        {
            if (unitTransform == null) continue;
            Unit unitScript = unitTransform.GetComponent<Unit>();
            if(unitScript == null) continue;

            if (unitScript.unitId == unitScriptToPlace.unitId)
            {
                Destroy(unitTransform.gameObject);
            }
        }

        //Remove satelite from the list of units in orbit
        unitsInOrbit.Remove(unitScriptToPlace);

        //Update unitsinorbit UI
        UpdateUnitInOrbitView();

        //Go back to selecting
        GameStateMachine.Instance?.SetState(GameplayStateId.Selecting);
    }

    public void PlaceBuildingOnSurface(Vector2Int coordClicked)
    {
        //Remove buliding from inventory.
        if (unitScriptToPlace == null)
        {
            Debug.LogWarning("No building selected to place.");
            return;
        }

        var itemDef = unitScriptToPlace.shipRuntimeData?.itemDefinition;

        if (itemDef != null && itemSourceShip.unitInventory != null)
        {
            itemSourceShip.unitInventory.RemoveItem(new ItemInstance { ItemDefinition = itemDef }, 1);
        }
        else
        {
            Debug.LogWarning("Could not find the building item in the source inventory.");
        }

        GameStateMachine.Instance?.SetState(GameplayStateId.Selecting);
    }

    public void ToggleOreMap()
    {
        if (asteroidBackgroundRenderer == null)
        {
            Debug.LogError("Asteroid background renderer is not assigned.");
            return;
        }

        // Toggle the visibility of the asteroid background
        if(isOreMapDisplayed)
        {
            // Hide the ore map and show the normal asteroid view
            isOreMapDisplayed = false;
            asteroidBackgroundRenderer.sprite = currentAsteroidComponent.asteroidIcon; // Assuming you have a different sprite for the ore map, you can toggle between them here.
        }
        else
        {
            // Show the ore map and hide the normal asteroid view
            isOreMapDisplayed = true;
            asteroidBackgroundRenderer.sprite = currentAsteroidComponent.GetComponent<MinableComponent>().oreMap; // Assuming you have a different sprite for the ore map, you can toggle between them here.

        }

    }
}
