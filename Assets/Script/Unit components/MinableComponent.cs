using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class MinableComponent : MonoBehaviour
{
    [Header("Tiles")]
    public List<TileData> tilesData = new List<TileData>();
    // replaced per-request: use canonical hex geometry instead of nrOfTiles
    [Tooltip("Hex 'radius' in cells (cube distance). World will be a hex of this radius.")]
    public int worldRadius = 2;

    [Tooltip("Hex size in world units (center to corner distance).")]
    public float hexSize = 1f;

    public Sprite backgroundSprite;
    public int scrollLimit;

    [Header("Resource")]
    public List<InventoryItemAmount> minableResourceList = new List<InventoryItemAmount>();
    public int totalAmount = 0;      // remaining resource in the ground
    public int storedAmount = 0;     // produced and ready for pickup (via outposts)
    public float veinsAmount = 0;
    public float patchesAmount = 0;

    // Missing multiplier used by ExtractUsingTool — added here.
    [Tooltip("Multiplier applied to module mining speed when extracting from this minable.")]
    public float deliverMultiplier = 1f;

    public event Action OnSurveyed;
    public event Action<int> OnOutpostPlaced; // slotIndex
    public event Action OnDepleted;
    public event Action OnProduced; // fired when outpost production added to storedAmount
    public event Action<Vector2Int> OnTileSurveyedByAxial; // axial (q,r)

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnNewTurn += HandleNewTurn;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnNewTurn -= HandleNewTurn;
    }

    private void Start()
    {
        // Initialize tiles if not already set
        if (tilesData.Count == 0)
        {
            // Build axial region from worldRadius and create tile entries in canonical order
            var cells = HexMath.AxialRegion(worldRadius);
            // Sort by ring distance then q then r - matches other consumers
            cells.Sort((a, b) =>
            {
                int da = Mathf.Max(Mathf.Abs(a.x), Mathf.Abs(a.y), Mathf.Abs(-a.x - a.y));
                int db = Mathf.Max(Mathf.Abs(b.x), Mathf.Abs(b.y), Mathf.Abs(-b.x - b.y));
                if (da != db) return da.CompareTo(db);
                if (a.x != b.x) return a.x.CompareTo(b.x);
                return a.y.CompareTo(b.y);
            });

            foreach (var cell in cells)
            {
                var newTileData = new TileData {};
                // store axial indices in tileData (row = r, col = q)
                newTileData.tileIndexRow = cell.y;
                newTileData.tileIndexCol = cell.x;
                tilesData.Add(newTileData);
            }
        }
    }

    // Called per player turn (Option A). Produces resources from outposts into storedAmount.
    private void HandleNewTurn()
    {
        //ProduceFromOutposts();
    }

    // Unit-based extraction: compute extraction amount based on module mining speed and site multiplier.
    // This is the operation used by MineAction for manual mining by a unit.
    // Returns how many units were extracted from ground (not from storedAmount).
    public int ExtractUsingTool(int moduleMiningSpeed)
    {
        if (moduleMiningSpeed <= 0 || totalAmount <= 0) return 0;

        int extracted = Mathf.FloorToInt(moduleMiningSpeed * deliverMultiplier);
        extracted = Mathf.Max(0, extracted);
        extracted = Mathf.Min(extracted, totalAmount);
        totalAmount -= extracted;

        if (totalAmount <= 0) OnDepleted?.Invoke();

        return extracted;
    }

    // Units (or UI) can withdraw produced (storedAmount) that outposts produced.
    // Returns how many units actually taken.
    public int TryWithdrawStored(int amount)
    {
        if (amount <= 0 || storedAmount <= 0) return 0;
        int take = Mathf.Min(amount, storedAmount);
        storedAmount -= take;
        return take;
    }

    // Reveal by axial coordinate (q,r). Updates tilesData and raises event.
    public void RevealTileByAxial(Vector2Int axial)
    {
        // find slot index that matches axial (q = col, r = row) — ensure the axial exists in data
        int idx = tilesData.FindIndex(t => t.tileIndexCol == axial.x && t.tileIndexRow == axial.y);
        if (idx < 0) return;

        // Try to find a corresponding MiningObjectTileData instance in the scene (children of this Minable or elsewhere).
        // We only change the visual fog GameObject; we do NOT modify the data model (tilesData) or fire events.
        var views = MiningUIManager.Instance.tileParent.GetComponentsInChildren<MiningObjectTileData>(true);
        foreach (var v in views)
        {
            if (v == null || v.tileData == null) continue;
            if (v.tileData.tileIndexCol == axial.x && v.tileData.tileIndexRow == axial.y)
            {
                if (v.fogGO != null)
                {
                    v.fogGO.SetActive(false);
                }
                // found the visual; stop searching
                return;
            }
        }

        // If no visual instance was found, do nothing to data or events.
        // (Optional) you can log for debugging:
        // Debug.Log($"RevealTileByAxial: no instantiated tile view found for axial {axial} on Minable '{name}'.");

    }

    public void SurvayTile(Vector2Int axial)
    {
        // find slot index that matches axial (q = col, r = row)
        int idx = tilesData.FindIndex(t => t.tileIndexCol == axial.x && t.tileIndexRow == axial.y);
        if (idx < 0) return;
        var td = tilesData[idx];
        if (td.isSurveyed) return; // already revealed
        td.isSurveyed = true;
        // notify listeners (UI, other systems)
        OnTileSurveyedByAxial?.Invoke(axial);
    }

    // Convenience: reveal by world position (uses this minable's hexSize)
    public void RevealTileByWorldPos(Vector3 worldPos)
    {
        var axial = HexMath.GetGridPosFromWorldPos(new Vector3(worldPos.x, worldPos.y, worldPos.z), hexSize);
        RevealTileByAxial(axial);
    }

    // Helper: get number of outposts currently active
    public int GetOutpostCount()
    {
        int c = 0;
        // Count placed outposts from the tiles list (tiles contain TileData with outpost flags)
        if (tilesData != null)
        {
            foreach (var t in tilesData)
            {
                if (t == null) continue;
                if (t.hasMiningOutpost && !t.hasAbandonMiningOutpost) c++;
            }
        }
        return c;
    }

#if UNITY_EDITOR
    // Context menu to populate tiles in editor so that tiles match the hex defined by worldRadius.
    [ContextMenu("Populate Tiles From Radius")]
    public void PopulateTilesFromRadiusEditor()
    {
        // Build canonical cell list for the radius
        var cells = HexMath.AxialRegion(worldRadius);
        // Sort by ring distance then q then r - canonical ordering
        cells.Sort((a, b) =>
        {
            int da = Mathf.Max(Mathf.Abs(a.x), Mathf.Abs(a.y), Mathf.Abs(-a.x - a.y));
            int db = Mathf.Max(Mathf.Abs(b.x), Mathf.Abs(b.y), Mathf.Abs(-b.x - b.y));
            if (da != db) return da.CompareTo(db);
            if (a.x != b.x) return a.x.CompareTo(b.x);
            return a.y.CompareTo(b.y);
        });

        // Populate tilesData list to match the canonical cell list
        tilesData = new List<TileData>(cells.Count);
        foreach (var cell in cells)
        {
            var td = new TileData
            {
                tileIndexCol = cell.x,
                tileIndexRow = cell.y,
                isSurveyed = false,
                hasMiningOutpost = false,
                hasAbandonMiningOutpost = false,
                oreAmount = 0f,
                isPlayerTurnOver = false,
                isBlocked = false
                // other TileData fields keep their defaults
            };
            tilesData.Add(td);
        }

        EditorUtility.SetDirty(this);
        Debug.Log($"Populated {tilesData.Count} mining TileData entries for Minable '{gameObject.name}' (radius {worldRadius}).");
    }

    // Context menu to clear tiles created by the editor tool.
    [ContextMenu("Clear Tiles (Editor)")]
    private void ClearTilesEditor()
    {
        tilesData = new List<TileData>();
        EditorUtility.SetDirty(this);
        Debug.Log($"Cleared mining tiles for Minable '{gameObject.name}'.");
    }
#endif
}