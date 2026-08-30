using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MiningUIManager : MonoBehaviour
{
    public static MiningUIManager Instance { get; private set; }

    [Header("Grid")]
    public GameObject tilePrefab;       // small UI button prefab representing a surface slot
    public Transform tileParent;   // parent in the UI canvas to place slots into 
    public HexGridComponent uiHexGrid;              // authoritative grid used for UI layout
    public float uiPixelsPerWorldUnit = 100f;      // how many UI pixels equal 1 world unit

    // Reintroduced: allow specifying blocked axial coords in inspector
    [Header("Blocked Tiles (axial q,r)")]
    [Tooltip("List of blocked tile axial coordinates to disable in the UI (q,r).")]
    public List<Vector2Int> blockedCells = new List<Vector2Int>();

    public MinableComponent currentMinable;
    private readonly List<GameObject> spawnedTiles = new();
    private readonly Stack<GameObject> tilePool = new(); // pool for recycling tiles
    private readonly Dictionary<Vector2Int, GameObject> spawnedTileMap = new();

    public Unit selectedOrbitalUnit;

    [Header("Reference")]
    public GameObject Game_View;
    public GameObject MinabelObject_View;
    public HexGridComponent minableObjectView_hexGridComponent;
    public GameObject gameHexGrid;
    public HexGridComponent gameView_hexGridComponent;
    public GameObject orbitingUnitView;
    public GameObject orbitingUnitPrefab;
    public Transform orbitingUnitsListContent;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Start()
    {
        orbitingUnitView.SetActive(false);
    }

    public void HideMiningUI()
    {
        // Hide minable object view
        MinabelObject_View.SetActive(false);
        Game_View.SetActive(true);

        //Tell what hex to draw in the game view.
        HexGridManager.Instance.SetActiveGrid(gameView_hexGridComponent); 

        // return UI tiles to pool (keeps them for next open)
        spawnedTileMap.Clear();
        ReturnAllTilesToPool();

        orbitingUnitView.SetActive(false);

        GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
    }

    // Show a minable in the UI (call when selecting an asteroid/planet)
    public void ShowMinable(MinableComponent minable)
    {
        // Set the current minable to the one passed in
        currentMinable = minable;
        if (currentMinable == null) return;

        // Show minableobject view
        MinabelObject_View.SetActive(true);
        Game_View.SetActive(false);

        //Set active hexgrid
        HexGridManager.Instance.SetActiveGrid(minableObjectView_hexGridComponent);

        //Clear all path and markers from the hexgrid.
        HexGridManager.Instance.ClearMarkers();
        HexGridManager.Instance.ClearPath();

        // Populate UI (reuses pooled tiles)
        SpawnTiles(currentMinable);
        UpdateSummary();

        //Populate list with units in orbit.
        UpdateOrbitingUnitView();

        //Clear selection
        SelectionService.Instance.ClearSelection();

        //Hide buttons
        UIManager.Instance.HideAllUI();

        //Show turnebuttons
        UIManager.Instance.ShowTruenButtons();
    }

    public void UpdateOrbitingUnitView()
    {
        // Show view
        orbitingUnitView.SetActive(true);

        //Clear out the OrbitingView
        foreach (Transform child in orbitingUnitsListContent)
        {
            Destroy(child.gameObject);
        }

        // Get all units in orbit around the current minable
        var orbitingUnits = GameManager.Instance.GetPlayerUnitsInOrbit(currentMinable.transform.position);

        foreach (var orbitingUnit in orbitingUnits)
        {
            var orbUnit= Instantiate(orbitingUnitPrefab, orbitingUnitsListContent);
            var orbUnitComp = orbUnit.GetComponent<OrbitingUnitPrefab_Entery>();
            if (orbUnitComp != null)
            {
                orbUnitComp.SetUnit(orbitingUnit);
            }

        }
    }

    public void RemoveUnitFromOrbit(MiningObjectTileData tileToMoveTO)
    {
        //Move GO in hirarcy
        selectedOrbitalUnit.gameObject.transform.parent = tileParent.root;

        //Move GO in world space
        selectedOrbitalUnit.transform.position = tileToMoveTO.transform.position;

        //Set unit location to tile location
        selectedOrbitalUnit.unitLocationType = Unit.UnitLocationType.MinableObject;

        //Remove from orbiting units list
        UpdateOrbitingUnitView();

        //Remove FOG.
        tileToMoveTO.SetRevealed(true);

        //Set selected unit.
        SelectionService.Instance.ClearSelection();
        SelectionService.Instance.SetSelectedUnit(selectedOrbitalUnit);

        //Change to selecting unit state
        GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
    }

    private void UpdateSummary()
    {
        if (currentMinable == null) return;
        /*totalAmountText.text = $"Ground: {currentMinable.totalAmount}";
        storedAmountText.text = $"Stored: {currentMinable.storedAmount}";
        outpostCountText.text = $"Outposts: {currentMinable.GetOutpostCount()}";*/
    }

    // Pool helpers
    private GameObject GetPooledTile()
    {
        GameObject go;
        if (tilePool.Count > 0)
        {
            go = tilePool.Pop();
            go.SetActive(true);
            return go;
        }

        if (tilePrefab == null)
        {
            Debug.LogWarning("tilePrefab is null. Cannot create tile.");
            return null;
        }

        go = Instantiate(tilePrefab, tileParent);
        go.transform.localScale = Vector3.one;
        return go;
    }

    private void ReturnTileToPool(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        go.transform.SetParent(tileParent, false);
        tilePool.Push(go);
    }

    private void ReturnAllTilesToPool()
    {
        for (int i = spawnedTiles.Count - 1; i >= 0; i--)
        {
            var go = spawnedTiles[i];
            if (go == null) continue;
            ReturnTileToPool(go);
        }
        spawnedTiles.Clear();
    }

    // Instantiate or reuse tiles in a hex-shaped layout and parent them to tileParent.
    // Uses point-top axial coordinates. uiPixelsPerWorldUnit maps world units -> UI pixels.
    private void SpawnTiles(MinableComponent minable)
    {
        if (minable == null)
        {
            Debug.LogWarning("SpawnTiles called with null minable. Cannot spawn slots.");
            return;
        }

        if (tilePrefab == null) Debug.LogWarning("tilePrefab is null. Cannot spawn slots.");
        if (tileParent == null) Debug.LogWarning("tileParent is null. Cannot spawn slots.");

        // return any previously spawned tiles to pool
        ReturnAllTilesToPool();

        // Resolve authoritative hex size to use for placement so UI spacing matches the game grid:
        // precedence: sourceHexGrid -> uiHexGrid -> minable
        HexGridComponent authoritativeGrid = HexGridManager.Instance.CurrentHexGridComponent != null ? HexGridManager.Instance.CurrentHexGridComponent : uiHexGrid;
        float authoritativeHexSize;
        if (authoritativeGrid != null)
        {
            if (authoritativeGrid.useMinableComponentHexGridSize && authoritativeGrid.minableReference != null)
                authoritativeHexSize = authoritativeGrid.minableReference.hexSize;
            else
                authoritativeHexSize = authoritativeGrid.hexSize;
        }
        else
        {
            authoritativeHexSize = (minable != null) ? minable.hexSize : 1f;
        }

        // Determine effective pixels-per-world-unit for the Canvas that contains tileParent.
        // We factor in Canvas.referencePixelsPerUnit so a common PPU (100) doesn't multiply spacing unexpectedly.
        var canvas = tileParent.GetComponentInParent<Canvas>();
        float canvasRefPixelsPerUnit = (canvas != null) ? canvas.referencePixelsPerUnit : 100f;
        float pixelsPerWorldUnit = uiPixelsPerWorldUnit / canvasRefPixelsPerUnit;

        // Use canonical generator so ordering/radius match other systems
        var cells = HexMath.GenerateCellsForCountSorted(minable.tilesData.Count > 0 ? minable.tilesData.Count : HexMath.CellsInRadius(minable.worldRadius));
        int count = cells.Count;

        for (int i = 0; i < count; i++)
        {
            var axial = cells[i];

            // Compute world center using authoritative hex size, then map to UI units (anchoredPosition)
            Vector2 worldCenter = HexMath.AxialToWorldCenter_PointTop(axial, authoritativeHexSize);
            Vector2 uiCenter = worldCenter * pixelsPerWorldUnit;

            // Acquire tile from pool or instantiate
            var go = GetPooledTile();
            if (go == null) continue;

            go.name = $"Slot_{axial.x}_{axial.y}_{i}";

            // Ensure it's parented to tileParent
            go.transform.SetParent(tileParent, false);

            // Store and configure tile data component
            spawnedTiles.Add(go);
            spawnedTileMap[new Vector2Int(axial.x, axial.y)] = go;
            var miningObjectTileData = go.GetComponent<MiningObjectTileData>();
            if (miningObjectTileData != null)
            {
                miningObjectTileData.tileData.tileIndexRow = axial.y;
                miningObjectTileData.tileData.tileIndexCol = axial.x;

                // Determine blocked state from manager's blockedCells list
                bool isBlocked = blockedCells != null && blockedCells.Contains(axial);
                miningObjectTileData.tileData.isBlocked = isBlocked;

                // Protect against minable.tiles index overflow; assume minable.tiles exists and is ordered
                if (i < minable.tilesData.Count)
                {
                    miningObjectTileData.tileData.isSurveyed = minable.tilesData[i].isSurveyed;
                    // If Minable provided tile has its own block flag, respect it as well
                    miningObjectTileData.tileData.isBlocked |= minable.tilesData[i].isBlocked;
                }

                //Set FOG GO
                miningObjectTileData.fogGO = go.transform.GetChild(0).gameObject;

                // set fog GO visibility if the prefab contains it
                if (miningObjectTileData.fogGO != null)
                {
                    // show fog when not surveyed or when blocked
                    miningObjectTileData.fogGO.SetActive(!miningObjectTileData.tileData.isSurveyed || miningObjectTileData.tileData.isBlocked);
                }

                // If prefab has a Button component, disable it when blocked
                var btn = go.GetComponent<Button>();
                if (btn != null) btn.interactable = !miningObjectTileData.tileData.isBlocked;
            }

            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchoredPosition = uiCenter;
            }

            // Optional: label the slot if prefab contains TMP_Text
            var texts = go.GetComponentsInChildren<TMP_Text>();
            if (texts.Length > 0) texts[0].text = $"#{i}";
        }
    }

    public void HandleTileClick(TileData tileData)
    {
        if (tileData == null) return;
        // Check if the tile is blocked
        if (tileData.isBlocked)
        {
            Debug.Log($"Tile at Row: {tileData.tileIndexRow}, Col: {tileData.tileIndexCol} is blocked. Cannot click.");
            return;
        }
        // Check if the tile is surveyed
        if (!tileData.isSurveyed)
        {
            Debug.Log($"Tile at Row: {tileData.tileIndexRow}, Col: {tileData.tileIndexCol} is not surveyed. Cannot click.");
            return;
        }
        // Check if the tile has a mining outpost
        if (tileData.hasMiningOutpost)
        {
            Debug.Log($"Tile at Row: {tileData.tileIndexRow}, Col: {tileData.tileIndexCol} has a mining outpost. Cannot click.");
            return;
        }
        // Proceed with handling the tile click
        Debug.Log($"Tile at Row: {tileData.tileIndexRow}, Col: {tileData.tileIndexCol} clicked successfully.");

        // Implement further logic for handling the tile click, such as deploying satellites or other actions.

        currentMinable.RevealTileByAxial(new Vector2Int(tileData.tileIndexCol, tileData.tileIndexRow));
    }
    public void ClearUI()
    {
        // return all spawned tiles to pool (do not destroy)
        ReturnAllTilesToPool();
    }
}