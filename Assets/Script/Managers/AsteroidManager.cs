using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
using System;
using System.IO;


public class AsteroidManager : MonoBehaviour
{
    public AsteroidComponent currentAsteroid;
    public Transform uintInOrbitView;
    public Transform unitInOrbitListContent;
    public GameObject unitInOrbitPrefab;

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

        // 3) Save which units are in the orbit/current view, if you need to restore them later
        //    This is only for later restoration, not for fog registration.
        unitsInOrbit = TileManager.Instance != null
            ? TileManager.Instance.GetUnitsOnTile(asteroid.currentTileCoord)
            : new List<Unit>();

        // 3.5) SaveAllUnits and SaveAllInventories to ensure the current state is preserved before switching views
        SaveManager.Instance?.SaveAllUnits(GameManager.Instance.gameViewUnitsSaveFile);
        SaveManager.Instance?.SaveAllInventories();

        // 3.6) Save all Asteroid data
        SaveManager.Instance?.SaveAllAsteroids(GameManager.Instance.gameViewAsteroidsSaveFile);

        // 4) Hide all active game-view units and asteroids
        SaveManager.Instance?.HideGameViewObjects();

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
        currentAsteroid = asteroid;

        // 9) Set state
        GameStateMachine.Instance?.SetState(GameplayStateId.Selecting);

        // 10) populate the orbital units UI
        uintInOrbitView.transform.gameObject.SetActive(true);
        AddUnitToOrbit();

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
        if (currentAsteroid != null)
        {
            TileManager.Instance?.SaveFogState(currentAsteroid.asteroidTileSatusSave);
        }

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

        Debug.Log("Returned from asteroid view.");
    }

    public void AddUnitToOrbit()
    {
        //Clear Lsit
        foreach (Transform child in unitInOrbitListContent)
        {
            Destroy(child.gameObject);
        }

        //Populate the list with units that are currently in orbit
        foreach (var unit in unitsInOrbit)
        {
            if (unit == null) continue;
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
}
