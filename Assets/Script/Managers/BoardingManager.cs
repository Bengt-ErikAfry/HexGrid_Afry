using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class BoardingManager : MonoBehaviour
{
    public static BoardingManager Instance { get; private set; }

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

    [Header("Reference")]
    public GameObject boardingObject_view;
    public GameObject Game_View;
    public HexGridComponent boardingObjectView_hexGridComponent;
    public GameObject gameHexGrid;
    public HexGridComponent gameView_hexGridComponent;
    public GameObject orbitingUnitView;
    public GameObject orbitingUnitPrefab;
    public Transform orbitingUnitsListContent;
    public GameObject moduleView;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ShowInteriorView()
    {         // Implement logic to show the interior view
        Debug.Log("Showing Interior View");

        //Clear all path and markers from the hexgrid.
        TileManager.Instance.ClearHighlights();
        TileManager.Instance.ClearPath();

        //Hide buttons
        UIManager.Instance.HideAllUI();

        //Close the modulevie
        InfoScreenManager.Instance.CloseModuleView();

        //To detect where to place the boarding enterence point.
        GameStateMachine.Instance.SetState(GameplayStateId.Selecting);

        //Load the interior ship layout into the hexgrid.
        string path = Path.Combine(Application.streamingAssetsPath, "hex_ShipIntrior_test.json");
        TileManager.Instance.LoadLayoutFromFile(path);
    }

    public void HideBoardingView()
    {         // Implement logic to hide the boarding view
        Debug.Log("Hiding Boarding View");

        // Hide minable object view
        boardingObject_view.SetActive(false);
        Game_View.SetActive(true);

        //Tell what hex to draw in the game view.
        HexGridManager.Instance.SetActiveGrid(gameView_hexGridComponent);

        // return UI tiles to pool (keeps them for next open)
        spawnedTileMap.Clear();
        ReturnAllTilesToPool();

        orbitingUnitView.SetActive(false);

        GameStateMachine.Instance.SetState(GameplayStateId.Selecting);
    }

    //////////////////////////////////////////// Pool helpers
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

}
