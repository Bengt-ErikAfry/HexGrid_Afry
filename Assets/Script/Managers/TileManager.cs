using UnityEngine;
using UnityEngine.LightTransport;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class TileManager : MonoBehaviour
{
    public Transform tilesParent;
    // Radius used as fallback to find nearest tile when overlap doesn't hit (world units)
    public float nearestTileSearchRadius = 0.5f;

    public static TileManager Instance { get; private set; }

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

    public (Vector2Int) GetTileCoordinateFromWorldPosition(Vector3 worldPosition)
    {
        // SceneTiles mode:
        var world2 = new Vector2(worldPosition.x, worldPosition.y);

        // 3) Fallback: search children of tilesParent (if assigned) for the nearest tile with a coordinate
        float bestDist = float.MaxValue;
        Vector2Int bestCoord = Vector2Int.zero;
        bool found = false;

        foreach (Transform tileTransform in tilesParent)
        {
            float d = Vector2.SqrMagnitude((Vector2)tileTransform.position - world2);
            if (d < bestDist && d <= nearestTileSearchRadius * nearestTileSearchRadius)
            {
                var coord = TryReadTileCoordinateFromComponent(tileTransform.gameObject);
                if (coord.HasValue)
                {
                    bestDist = d;
                    bestCoord = coord.Value;
                    found = true;
                }
            }
        }

        //Return coordinate of the nearest tile if found, otherwise return Vector2Int.zero
        if (found) return (bestCoord);
        return null;
    }

    private Vector2Int? TryReadTileCoordinateFromComponent(GameObject go)
    {
        if (go == null) return null;

        // 1) Try an attached component named TileData (most likely)
        var tileComp = go.GetComponent<HexGrid_TilePrefab>();
        if (tileComp != null)
        {
            Vector2Int result = new Vector2Int(tileComp.tileIndexRow, tileComp.tileIndexCol);
            return result;
        }
        else
        {
            Debug.LogWarning($"GameObject {go.name} does not have a TileData component. Ensure that the tile GameObject has the TileData script attached.");
        }
        return null;
    }

    public void ClearHighlightedTiles()
    {
        foreach (Transform tileTransform in tilesParent)
        {
            var tileComp = tileTransform.GetComponent<HexGrid_TilePrefab>();
            if (tileComp != null)
            {
                tileComp.SetHighlight(false);
            }
        }
    }

    //Highlight tile att screenpos and unhide all other tiles
    public void HighlightTileUnderScreenPosition(Vector2 screenPosition)
    {
        // Convert screen position to world position
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Camera.main.nearClipPlane));
        // Get the tile coordinate from the world position
        Vector2Int tileCoord = GetTileCoordinateFromWorldPosition(worldPosition);
        // Highlight the tile with the obtained coordinate
        foreach (Transform tileTransform in tilesParent)
        {
            var tileComp = tileTransform.GetComponent<HexGrid_TilePrefab>();
            if (tileComp != null)
            {
                if (tileComp.tileIndexRow == tileCoord.x && tileComp.tileIndexCol == tileCoord.y)
                {
                    tileComp.SetHighlight(true);
                }
                else
                {
                    tileComp.SetHighlight(false);
                }
            }
        }
    }
}
