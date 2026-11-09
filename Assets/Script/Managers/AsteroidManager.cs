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
    public Transform gameViewUnitContainer; //A container for all units in gameview when asteroid view is open.
    public Transform gameViewAsteroidContainer; //A container for all asteroids in gameview when asteroid view is open.
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
            TileManager.Instance?.SaveTileState(GameManager.Instance.gameViewLayout);
        }

        // 3) save all units in orbit.
        unitsInOrbit = TileManager.Instance?.GetUnitsOnTile(asteroid.currentTileCoord) ?? new List<Unit>();

        // 3.1) set current asteroid
        currentAsteroidComponent = asteroid;
        currentMinableComponent = asteroid.GetComponent<MinableComponent>();

        // 3.5) SaveAllUnits and SaveAllInventories to ensure the current state is preserved before switching views
        //SaveManager.Instance?.SaveAllUnits(GameManager.Instance.gameViewUnitsSaveFile, asteroid);
        //SaveManager.Instance?.SaveAllInventories();

        // 3.6) Save all Asteroid data
        //SaveManager.Instance?.SaveAllAsteroids(GameManager.Instance.gameViewAsteroidsSaveFile);

        // 4) Hide all active game-view units and asteroids
        //SaveManager.Instance?.HideGameViewObjects(asteroid);
        MoveAndHideAllGameviewUnits();

        // 5) Load the asteroid template layout and runtime state from the asteroid's template/save files.
        asteroid.LoadTemplateAndRuntimeState();

        // 6) Load asteroid-surface units
        //SaveManager.Instance?.LoadAsteroidViewObject(asteroid);
        ShowAsteroidUnits();

        // 7) Register only the units that are actually active in this view
        //    This prevents hidden game-view units from still contributing to fog/visibility.
        /*if (GameManager.Instance != null)
        {
            foreach (var unit in GameManager.Instance.playerUnits)
            {
                if (unit == null) continue;
                if (!unit.gameObject.activeSelf) continue;

                int range = Mathf.CeilToInt(unit.detectionRange);
                TileManager.Instance?.RegisterUnit(unit, range);
            }
        }*/

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

        // 3) Save asteroid fog state before leaving and persist the mutable asteroid runtime state (ore + fog)
        if (currentAsteroidComponent != null)
        {
            currentAsteroidComponent.SaveTileRuntimeState();
            //TileManager.Instance?.SaveTileState(currentAsteroidComponent.asteroidTileSatusSave);
        }

        // 3.5 ) SaveAllUnits and SaveAllInventories to ensure the current state is preserved before switching views
        //SaveManager.Instance?.SaveAllUnits(currentAsteroidComponent.unitsOnSurfaceSave);
        //SaveManager.Instance?.SaveAllInventories();

        // 4) Hide all active game-view units and asteroids
        //SaveManager.Instance?.HideAsteroidViewObjects();
        MoveAndHideAllAsteroidUnits();

        // 4) Restore the original game-view layout
        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.gameViewLayout))
        {
            string path = Path.Combine(Application.streamingAssetsPath, GameManager.Instance.gameViewLayout);
            TileManager.Instance?.LoadLayoutFromFile(path);
        }

        // 5) Restore fog state for the game view
        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.gameViewLayout))
        {
            TileManager.Instance?.LoadTileState(GameManager.Instance.gameViewLayout);
        }

        // 6) Re-show the original game-view objects
        //SaveManager.Instance?.LoadAllUnits(GameManager.Instance.gameViewUnitsSaveFile);
        ShowAllGameViewUnits();

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

    public void MoveAndHideAllGameviewUnits()
    {
        if (GameManager.Instance == null) return;
        foreach (var unit in GameManager.Instance.unitsList.ToList())
        {
            if (unit == null) continue;
            if (!unit.gameObject.activeSelf) continue;
            // Move the unit to the hidden parent
            unit.transform.SetParent(gameViewUnitContainer, false);
            unit.transform.localPosition = Vector3.zero; // Optional: Reset position if needed
            unit.transform.GetChild(0).gameObject.SetActive(false); // Hide the visual representation
            GameManager.Instance.unitsList.Remove(unit); // Remove from the main list to avoid duplicates
            if(unit.isPlayerControlled)
            {
                GameManager.Instance.playerUnits.Remove(unit);
            }
            else
            {
                GameManager.Instance.enemyUnits.Remove(unit);
            }
        }
        foreach (var asteroid in GameManager.Instance.asteroidsList.ToList())
        {
            if (asteroid == null) continue;
            if (!asteroid.gameObject.activeSelf) continue;
            // Move the unit to the hidden parent
            asteroid.transform.SetParent(gameViewAsteroidContainer, false);
            asteroid.transform.localPosition = Vector3.zero; // Optional: Reset position if needed
            asteroid.transform.GetChild(0).gameObject.SetActive(false); // Hide the visual representation
            GameManager.Instance.asteroidsList.Remove(asteroid); // Remove from the main list to avoid duplicates
        }
    }

    public void MoveAndHideAllAsteroidUnits()
    {
        if (GameManager.Instance == null) return;
        foreach (var unit in GameManager.Instance.unitsList)
        {
            if (unit == null) continue;
            if (!unit.gameObject.activeSelf) continue;
            // Move the unit to the hidden parent
            unit.transform.SetParent(currentAsteroidComponent.transform, false);
            unit.transform.localPosition = Vector3.zero; // Optional: Reset position if needed
            unit.transform.GetChild(0).gameObject.SetActive(false); // Hide the visual representation
            GameManager.Instance.unitsList.Remove(unit); // Remove from the main list to avoid duplicates
        }
    }

    public void ShowAllGameViewUnits()
    {
        if (GameManager.Instance == null) return;
        foreach (Transform unitTF in gameViewUnitContainer)
        {
            if (unitTF == null) continue;
            if (!unitTF.gameObject.activeSelf) continue;

            var unit = unitTF.gameObject.GetComponent<Unit>();
            if (unit == null) continue;
            // Move the unit back to the game view parent
            unit.transform.SetParent(GameManager.Instance.unitInGameview, false);
            unit.transform.localPosition = TileManager.Instance.AxialToWorldCenter(unit.currentTileCoord);
            unit.transform.GetChild(0).gameObject.SetActive(true); // Show the visual representation
            GameManager.Instance.unitsList.Add(unit); // Add back to the main list
            if(unit.isPlayerControlled)
            {
                GameManager.Instance.playerUnits.Add(unit);
            }
            else
            {
                GameManager.Instance.enemyUnits.Add(unit);
            }
        }
        foreach (Transform asteroidTF in gameViewAsteroidContainer)
        {
            if (asteroidTF == null) continue;
            if (!asteroidTF.gameObject.activeSelf) continue;

            var asteroid = asteroidTF.gameObject.GetComponent<AsteroidComponent>();
            if (asteroid == null) continue;
            // Move the unit back to the game view parent
            asteroid.transform.SetParent(GameManager.Instance.asteroidsInGameView, false);
            asteroid.transform.localPosition = TileManager.Instance.AxialToWorldCenter(asteroid.currentTileCoord);
            asteroid.transform.GetChild(0).gameObject.SetActive(true); // Show the visual representation
            GameManager.Instance.asteroidsList.Add(asteroid); // Add back to the main list
        }
    }

    public void ShowAsteroidUnits()
    {
        if (currentAsteroidComponent == null)
        {
            Debug.LogWarning("ShowAsteroidUnits: currentAsteroidComponent is null.");
            return;
        }

        foreach (var unit in currentAsteroidComponent.unitsOnSurface)
        {
            if (unit == null) continue;
            if (!unit.gameObject.activeSelf) continue;
            unit.transform.SetParent(GameManager.Instance.unitInGameview, false);
            GameManager.Instance.unitsList.Add(unit);
            if (unit.isPlayerControlled)
            {
                GameManager.Instance.playerUnits.Add(unit);
            }
            else
            {
                GameManager.Instance.enemyUnits.Add(unit);
            }
            unit.transform.localPosition = TileManager.Instance.AxialToWorldCenter(unit.currentTileCoord);
            unit.transform.GetChild(0).gameObject.SetActive(true);
        }
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

        //Add building to the surface
        TileManager.Instance.AddUnitToTile(unitScriptToPlace, coordClicked);

        //Add to unit list
        GameManager.Instance.unitsList.Add(unitScriptToPlace);

        //add mining caoacity to the asteroid based on the building's mining module
        UpdateMiningAmount();

        //Update unitsinorbit UI
        UpdateUnitInOrbitView();

        //Set state back to selecting after placing the building
        GameStateMachine.Instance?.SetState(GameplayStateId.Selecting);
    }

    public void UpdateMiningAmount()
    {
        if (currentAsteroidComponent == null || unitScriptToPlace == null)
        {
            Debug.LogWarning("Cannot update mining amount: currentAsteroidComponent or unitScriptToPlace is null.");
            return;
        }

        //Reset the list
        currentMinableComponent.currentAvalibuleMinableResourceList.Clear();


        //Set a temp variabel for ore
        float totalIronOre = 0f;
        float totalCopperOre = 0f;

        //Get the amount of ore in all miningtiles.
        foreach (var unit in GameManager.Instance.unitsList)
        {
            if (unit == null) continue;
            if(unit.unitType != UnitType.Building) continue;
            if(unit.shipRuntimeData == null) continue;
            if(unit.shipRuntimeData.itemDefinition == null) continue;
            if(unit.shipRuntimeData.itemDefinition.itemType == ItemType.MiningOutpost)
            {
                //Get the amount of ore in current tile.
                if (currentAsteroidComponent.runtimeState.TryGetValue(unit.currentTileCoord, out var tileState))
                {
                    totalIronOre += tileState.ironOreAmount;
                    totalCopperOre += tileState.copperOreAmount;
                }
            }
        }

        Debug.Log($"Total Iron Ore: {totalIronOre}, Total Copper Ore: {totalCopperOre}");
        //Add the mining capacity of the building to the asteroid's total mining capacity.
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
