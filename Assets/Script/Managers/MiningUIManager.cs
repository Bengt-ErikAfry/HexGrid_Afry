using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MiningUIManager : MonoBehaviour
{
    public static MiningUIManager Instance { get; private set; }

    [Header("Grid")]
    public GameObject tilePrefab;       // small UI button prefab representing a surface slot
    public RectTransform tileParent;   // parent in the UI canvas to place slots into
    public int nrOfSlots = 100;                // total number of slots to display (for testing, can be dynamic)
    public float slotSize = 128f;

    private MinableComponent currentMinable;
    private readonly List<GameObject> spawnedTiles = new();

    [Header("Reference")]
    public GameObject canvas_Game;
    public GameObject canvas_MiningUI;
    public GameObject gameHexGrid;
    public MinableComponent minableComponent;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void Start()
    {
        //SpawnTiles();
    }

    public void HideMiningUI()
    {        
        canvas_MiningUI.SetActive(false);
        canvas_Game.SetActive(true);
        gameHexGrid.SetActive(true);
        GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
    }

    // Show a minable in the UI (call when selecting an asteroid/planet)
    public void ShowMinable(MinableComponent minable)
    {
        currentMinable = minable;
        if (currentMinable == null) return;

        minableComponent =minable;   //for furture use.
        canvas_MiningUI.SetActive(true);
        canvas_Game.SetActive(false);
        gameHexGrid.SetActive(false);

        GameStateMachine.Instance.SetState(GameplayStateId.ShowMiningObjectUI);

        SpawnTiles(minable);
    }

    /*
    public void LoadAllTiles(MinableComponent minable)
    {
        if(minable == null)
        {
            Debug.LogWarning("LoadAllTiles called with null minable. Cannot load tiles."); return;
        }

        ClearUI();

        SpawnTiles();

        /*
        // Update each spawned tile to reflect the corresponding surface slot data
        for (int i = 0; i < minable.tiles.Count; i++)
        {
            var tileData = minable.tiles[i];
            var tileGO = spawnedTiles[i];
            var miningobjecttiledata = tileGO.GetComponent<MiningObjectTileData>();
            // Update the tileGO to reflect tileData (e.g., change color, text, etc.)
            // Optionally, change the visual appearance based on slotData

            miningobjecttiledata.fogGO.SetActive(!tileData.isSurveyed); // Show fog if not surveyed

            var image = tileGO.transform.GetChild(0).GetComponent<Image>();
            if (image != null)
            {
                image.color = tileData.isSurveyed ? Color.green : Color.red;
            }
        }
    }*/

    private void UpdateSummary()
    {
        if (currentMinable == null) return;
        /*totalAmountText.text = $"Ground: {currentMinable.totalAmount}";
        storedAmountText.text = $"Stored: {currentMinable.storedAmount}";
        outpostCountText.text = $"Outposts: {currentMinable.GetOutpostCount()}";*/
    }

    // Instantiate nrOfSlots prefabs in a hex-shaped layout and parent them to surfaceSlotsParent.
    // Uses point-top axial coordinates. slotSize is passed to the axial->world math to control spacing.
    private void SpawnTiles(MinableComponent minable)
    {
        if (tilePrefab == null) Debug.LogWarning("tilePrefab is null. Cannot spawn slots.");
        if (tileParent == null) Debug.LogWarning("tileParent is null. Cannot spawn slots.");
        if (minable.nrOfTiles <= 0) Debug.LogWarning("nrOfSlots is non-positive. Cannot spawn slots.");

        // Build axial cell list for a hex with minimal radius R containing at least nrOfSlots
        int R = 0;
        var cells = new List<Vector2Int>();
        while (true)
        {
            cells.Clear();
            for (int rAx = -R; rAx <= R; rAx++)
                for (int q = -R; q <= R; q++)
                {
                    int x = q, z = rAx, y = -x - z;
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y), Mathf.Abs(z)) <= R)
                        cells.Add(new Vector2Int(q, rAx));
                }

            if (cells.Count >= minable.nrOfTiles) break;
            R++;
            if (R > 200) break;
        }

        // Sort by distance to center so layout is compact
        cells.Sort((a, b) =>
        {
            int da = Mathf.Max(Mathf.Abs(a.x), Mathf.Abs(a.y), Mathf.Abs(-a.x - a.y));
            int db = Mathf.Max(Mathf.Abs(b.x), Mathf.Abs(b.y), Mathf.Abs(-b.x - b.y));
            if (da != db) return da.CompareTo(db);
            if (a.x != b.x) return a.x.CompareTo(b.x);
            return a.y.CompareTo(b.y);
        });

        int count = Mathf.Min(minable.nrOfTiles, cells.Count);

        // We want center-to-center neighbor distance == slotSize (in UI pixels).
        // For point-top hexes the neighbor center distance = cornerRadius * sqrt(3).
        // So choose cornerRadius = slotSize / sqrt(3).
        float sqrt3 = Mathf.Sqrt(3f);
        float cornerRadius = slotSize / sqrt3;

        // Convert axial -> UI local pixels via HexGridLinesBaker using cornerRadius.
        // Use direct pixel mapping (assumes surfaceSlotsParent pivot is centered and Canvas uses pixel units).
        for (int i = 0; i < count; i++)
        {
            var axial = cells[i];

            // Get center using HexGrid math (with cornerRadius that yields center spacing = slotSize)
            Vector2 center = HexGridLinesBaker.AxialToWorldCenter_PointTop(axial, cornerRadius);

            // Instantiate slot prefab under parent
            var go = Instantiate(tilePrefab, tileParent);
            go.name = $"Slot_{axial.x}_{axial.y}_{i}";
            spawnedTiles.Add(go);
            MiningObjectTileData miningObjectTileData = go.GetComponent<MiningObjectTileData>();
            miningObjectTileData.tileIndexRow = axial.y;
            miningObjectTileData.tileIndexCol = axial.x;    
            miningObjectTileData.isSurveyed = minable.tiles[i].isSurveyed;

            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;

                // Place using local UI coordinates — parent pivot should be centered for proper centering.
                // If your Canvas is Screen Space - Camera or uses a CanvasScaler, you may need to convert
                // world->screen->local with the canvas camera. This code assumes 1:1 pixel mapping.
                rt.anchoredPosition = new Vector2(center.x, center.y);

                // Do not modify rt.sizeDelta here so prefab retains its own size (128x149).
            }

            // Optional: label the slot if prefab contains TMP_Text
            var texts = go.GetComponentsInChildren<TMP_Text>();
            if (texts.Length > 0) texts[0].text = $"#{i}";

            /*
            // Hook up button (capture index)
            var btn = go.GetComponent<Button>();
            int idx = i;
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnSlotClicked(idx));
            }
            */
        }

        // Sanity advice to avoid surprising scaling
        if (tileParent.pivot != new Vector2(0.5f, 0.5f))
            Debug.LogWarning("surfaceSlotsParent pivot is not center. Use center pivot for correct positioning.");
        var parentCanvas = tileParent.GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            var scaler = parentCanvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler != null && scaler.uiScaleMode == UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize)
                Debug.Log("CanvasScaler is ScaleWithScreenSize — UI pixel sizes will vary with resolution. Use ConstantPixelSize for fixed pixel spacing.");
        }
    }

    /*
    private void OnSlotClicked(int index)
    {
        if (currentMinable == null) { Debug.LogWarning("currentMinable is null. Cannot process slot click."); return; }

        var slot = currentMinable.tiles[index];

        // If not surveyed, show survey option
        if (!slot.isSurveyed)
        {
            // Simple immediate survey for testing
            slot.isSurveyed = true;
            //currentMinable.StartSurvey();
            UpdateSummary();
            ShowMinable(currentMinable); // refresh UI
            return;
        }

        /*
        // If surveyed and no outpost -> attempt to place outpost using currently selected unit
        if (!slot.hasMiningOutpost)
        {
            var selected = SelectionService.Instance.SelectedUnit;
            if (selected == null)
            {
                MessageSystemManager.Instance.CreateMessage("No unit selected to place outpost.", "", Vector3.zero, Color.yellow);
                return;
            }

            // Example: require the selected unit to have a Deployable kit; UI should check that in real flow.
            bool placed = currentMinable.PlaceOutpost(index);
            if (placed)
            {
                MessageSystemManager.Instance.CreateMessage($"{selected.unitName} placed outpost.", "", selected.transform.position, Color.green);
                UpdateSummary();
                ShowMinable(currentMinable);
            }
            else
            {
                MessageSystemManager.Instance.CreateMessage("Cannot place outpost here.", "", selected.transform.position, Color.yellow);
            }
            return;
        }

        // If has outpost, show slot details (placeholder)
        MessageSystemManager.Instance.CreateMessage($"Slot {index}: Outpost present.", "", currentMinable.transform.position, Color.white);
    }*/

    public void ClearUI()
    {
        foreach (var go in spawnedTiles) Destroy(go);
        spawnedTiles.Clear();
    }

    public void DeploySatelites_Bt_pressed()
    {
        //Get saltelites nr from ship in orbit

        //Show messagebox with slider

    }

    public void OnDeploySatelites_OK()
    {
        //Get nr from slider
        //Deploy satelites to minable
    }
}