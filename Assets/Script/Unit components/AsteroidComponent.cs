using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using static HexGrid_TilePrefab;
using static UnityEditor.Progress;
using Random = UnityEngine.Random;

public class AsteroidComponent : MonoBehaviour
{
    [Serializable]
    public class TileRuntimeState
    {
        public int x;
        public int y;
        public float oreAmount;
        public float etchOreAmount;
        public float ironOreAmount;
        public float copperOreAmount;
        public bool hasMiningOutpost;
        public bool isSurveyed;
        public bool isBlocked;
        public string visibilityState;
    }

    [Serializable]
    public class AsteroidRuntimeSaveData
    {
        public string asteroidId;
        public string templateFile;
        public List<TileRuntimeState> tiles = new List<TileRuntimeState>();

        public bool TryGetValue(Vector2Int coord, out TileRuntimeState tileState)
        {
            foreach (var existingTile in tiles)
            {
                if (existingTile.x == coord.x && existingTile.y == coord.y)
                {
                    tileState = existingTile;
                    return true;
                }
            }

            tileState = null;
            return false;
        }
    }

    public string asteroidID;   //Used when saving insted of using a object reference.
    public string asteroidName; //For desplying in UI. Set in inspector.
    public Vector2Int currentTileCoord; //Set when the asteroid is spawned. Used to find the asteroid when loading a save file.
    public string asteroidTileSatusSave; //created when player first show the planet. Then stays the same.
    public string asteroidTemplateFile; // Template JSON used when the asteroid is first opened. Example: "asteroid_01_template.json"
    public string asteroidRuntimeSaveFile; // Runtime mutable save containing fog + ore + tile flags, saved in Application.persistentDataPath.
    public string unitsOnSurfaceSave; //created when player first show the planet. Then stays the same.
    public Sprite asteroidIcon; //For desplying in UI. Set in inspector.

    public List<Unit> unitsOnSurface = new List<Unit>(); //List of units on the surface of the asteroid. Used for saving and loading. Init when asteroid are loaded. Updated when units are added/removed.
    public AsteroidRuntimeSaveData runtimeState = new AsteroidRuntimeSaveData();

    private void Awake()
    {
        //Wait an let the asteroid spawn before getting the tile coord. Otherwise it will be 0,0.
        StartCoroutine(waitOneFrame());
    }

    public void Start()
    {
        //Add to GameManager list.
        if(!GameManager.Instance.asteroidsList.Contains(this))
        GameManager.Instance.asteroidsList.Add(this);
    }

    private IEnumerator waitOneFrame()
    {
        yield return null; // Wait for one frame
        currentTileCoord = TileManager.Instance.WorldToAxial(this.transform.position);
        StopCoroutine(waitOneFrame());
    }

    public IEnumerator InitAsteroid()
    {
        // Wait until TileManager exists and has built its lookup
        yield return new WaitUntil(() => TileManager.Instance != null && TileManager.Instance.HasBuiltLookup);

        //Set cordinates for this unit. Used for pathfinding and other tile-based logic.
        currentTileCoord = TileManager.Instance.WorldToAxial(this.transform.position);

        //Set ship icon in selection view
        this.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = asteroidIcon;

        //GameObject name for debuging
        this.gameObject.name = asteroidName + " (" + asteroidID + ")";
    }

    public string GetTemplatePath()
    {
        string templateName = string.IsNullOrWhiteSpace(asteroidTemplateFile)
            ? asteroidTileSatusSave
            : asteroidTemplateFile;

        return Path.Combine(Application.streamingAssetsPath, templateName);
    }

    public string GetRuntimeSavePath()
    {
        string runtimeName = string.IsNullOrWhiteSpace(asteroidRuntimeSaveFile)
            ? (string.IsNullOrWhiteSpace(asteroidTileSatusSave)
                ? (asteroidID ?? "asteroid_runtime.json") + ".json"
                : asteroidTileSatusSave + "_runtime.json")
            : asteroidRuntimeSaveFile;

        return Path.Combine(Application.persistentDataPath, runtimeName);
    }

    public void LoadTemplateAndRuntimeState()
    {
        if (TileManager.Instance == null)
        {
            Debug.LogWarning($"AsteroidComponent.LoadTemplateAndRuntimeState: TileManager is null for {asteroidName}.");
            return;
        }

        string templatePath = GetTemplatePath();
        if (string.IsNullOrWhiteSpace(templatePath) || !File.Exists(templatePath))
        {
            Debug.LogWarning($"AsteroidComponent: template file not found for {asteroidName}: {templatePath}");
            return;
        }

        TileManager.Instance.LoadLayoutFromFile(templatePath);

        runtimeState = new AsteroidRuntimeSaveData
        {
            asteroidId = asteroidID,
            templateFile = Path.GetFileName(templatePath)
        };
        PopulateRuntimeStateFromTemplate();

        string runtimePath = GetRuntimeSavePath();
        if (File.Exists(runtimePath))
        {
            try
            {
                var json = File.ReadAllText(runtimePath);
                var saved = JsonUtility.FromJson<AsteroidRuntimeSaveData>(json);

                if (saved != null && saved.tiles != null)
                {
                    bool hasOreData = false;
                    foreach (var item in saved.tiles)
                    {
                        if (item == null) continue;
                        if (item.ironOreAmount > 0f || item.copperOreAmount > 0f || item.oreAmount > 0f)
                        {
                            hasOreData = true;
                            break;
                        }
                    }

                    if (hasOreData)
                    {
                        runtimeState = saved;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"AsteroidComponent: failed to parse runtime state for {asteroidName}: {ex.Message}");
            }
        }

        //CaptureRuntimeStateFromTiles();
        ApplyRuntimeStateToTiles();
    }

    public void PopulateRuntimeStateFromTemplate()
    {
        string templatePath = GetTemplatePath();
        if (string.IsNullOrWhiteSpace(templatePath) || !File.Exists(templatePath))
        {
            Debug.LogWarning($"AsteroidComponent: template file not found when populating runtime state for {asteroidName}: {templatePath}");
            return;
        }

        runtimeState ??= new AsteroidRuntimeSaveData();
        runtimeState.asteroidId = asteroidID;
        runtimeState.templateFile = Path.GetFileName(templatePath);
        runtimeState.tiles ??= new List<TileRuntimeState>();
        runtimeState.tiles.Clear();

        try
        {
            var json = File.ReadAllText(templatePath);
            var template = JsonUtility.FromJson<TileManager.RuntimeHexTemplate>(json);
            if (template == null || template.cells == null)
            {
                Debug.LogWarning($"AsteroidComponent: template file for {asteroidName} did not contain any cells.");
                return;
            }

            foreach (var cell in template.cells)
            {
                float ironOre = cell.GetOreAmountForItem("Iron Ore");
                float copperOre = cell.GetOreAmountForItem("Copper ore");
                float oreValue = Mathf.Max(ironOre, copperOre);
                if (oreValue <= 0f)
                    oreValue = cell.oreAmount > 0f ? cell.oreAmount : cell.greenRatio;

                Debug.Log($"Template value before assign: x={cell.x}, y={cell.y}, iron={ironOre}, copper={copperOre}");

                runtimeState.tiles.Add(new TileRuntimeState
                {
                    x = cell.x,
                    y = cell.y,
                    oreAmount = oreValue,
                    etchOreAmount = 0f,
                    ironOreAmount = ironOre,
                    copperOreAmount = copperOre,
                    hasMiningOutpost = false,
                    isSurveyed = false,
                    isBlocked = false,
                    visibilityState = string.IsNullOrWhiteSpace(cell.state)
                        ? TileVisibilityState.Unexplored.ToString()
                        : cell.state
                });
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"AsteroidComponent: failed to read template values for {asteroidName}: {ex.Message}");
        }
    }

    public void CaptureRuntimeStateFromTiles()
    {
        if (TileManager.Instance == null || TileManager.Instance.tilesParent == null)
            return;

        runtimeState ??= new AsteroidRuntimeSaveData();
        runtimeState.asteroidId = asteroidID;
        runtimeState.templateFile = Path.GetFileName(GetTemplatePath());
        runtimeState.tiles ??= new List<TileRuntimeState>();
        runtimeState.tiles.Clear();

        foreach (Transform child in TileManager.Instance.tilesParent)
        {
            var tile = child.GetComponent<HexGrid_TilePrefab>();
            if (tile == null) continue;

            var tileState = new TileRuntimeState
            {
                x = tile.tileIndexCol,
                y = tile.tileIndexRow,
                oreAmount = Mathf.Max(tile.ironOreAmount, tile.copperOreAmount),
                etchOreAmount = 0f,
                ironOreAmount = tile.ironOreAmount,
                copperOreAmount = tile.copperOreAmount,
                hasMiningOutpost = false,
                isSurveyed = false,
                isBlocked = false,
                visibilityState = tile.tileVisibilityState.ToString()
            };

            runtimeState.tiles.Add(tileState);
        }
    }

    public void ApplyRuntimeStateToTiles()
    {
        if (TileManager.Instance == null || runtimeState == null)
            return;

        foreach (var tileState in runtimeState.tiles)
        {
            var coord = new Vector2Int(tileState.x, tileState.y);
            var tile = TileManager.Instance.GetTileAtCoord(coord);
            if (tile == null)
                continue;

            if (!string.IsNullOrWhiteSpace(tileState.visibilityState) &&
                Enum.TryParse<TileVisibilityState>(tileState.visibilityState, out var state))
            {
                tile.tileVisibilityState = state;
                tile.ApplyVisibilityVisuals();
            }

            tile.oreAmount = tileState.oreAmount;
            tile.ironOreAmount = tileState.ironOreAmount;
            tile.copperOreAmount = tileState.copperOreAmount;
            if (tileState.etchOreAmount > 0f)
                tile.oreAmount = tileState.etchOreAmount;
            else if (tileState.ironOreAmount > 0f)
                tile.oreAmount = tileState.ironOreAmount;
            else if (tileState.copperOreAmount > 0f)
                tile.oreAmount = tileState.copperOreAmount;
        }
    }

    public void SaveTileRuntimeState()
    {
        if (TileManager.Instance == null)
            return;

        CaptureRuntimeStateFromTiles();

        /*
        string runtimePath = GetRuntimeSavePath();
        string json = JsonUtility.ToJson(runtimeState, true);
        File.WriteAllText(runtimePath, json);
        */
    }
}