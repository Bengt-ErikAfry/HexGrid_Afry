using UnityEngine;
using UnityEngine.UI;
using static HexGrid_TilePrefab;

public class HexGrid_TilePrefab : MonoBehaviour
{
    public int tileIndexRow;
    public int tileIndexCol;
    public TileVisibilityState tileVisibilityState = TileVisibilityState.Unexplored;
    public GameObject tileHighlightGameObject;
    public GameObject tileVisibleStateGameObject;

    [Header("Edge blocking (point-top neighbor order)")]
    [Tooltip("Index 0..5 correspond to HexMath.NeighborDirs() (point-top). Check true to place a wall on that edge.")]
    public bool[] walls = new bool[6];

    // Computed mask (optional, kept in sync for fast checks)
    [HideInInspector]
    public int wallMask = 0;

    private void OnValidate()
    {
        // Keep wallMask in sync with the booleans so inspectors or runtime code can read either.
        int mask = 0;
        if (walls != null)
        {
            for (int i = 0; i < walls.Length && i < 6; i++)
                if (walls[i]) mask |= (1 << i);
        }
        wallMask = mask;
    }

    public bool HasWall(int dirIndex)
    {
        if (walls == null) return false;
        if (dirIndex < 0 || dirIndex >= walls.Length) return false;
        return walls[dirIndex];
    }

    public void SetHighlight(bool highlight, Color highlightColor)
    {
        if (tileHighlightGameObject != null)
        {
            tileHighlightGameObject.SetActive(highlight);
            // Additional logic for setting highlight color can be added here if needed
            tileHighlightGameObject.GetComponent<SpriteRenderer>().color = highlightColor;
        }
        else
        {
            Debug.LogWarning($"Tile at row {tileIndexRow}, col {tileIndexCol} does not have a tileHighlightGameObject assigned.");
        }
    }
    public void ApplyVisibilityVisuals()
    {
        // Example policy:
        // - Unexplored: hide tile content and use black/dark overlay
        // - Explored: show tile floor but not enemies; dim highlight
        // - Visible: show everything normally
        switch (tileVisibilityState)
        {
            case TileVisibilityState.Unexplored:
                if (tileHighlightGameObject != null) tileVisibleStateGameObject.GetComponent<SpriteRenderer>().color = ColorManager.Instance.tile_UnExplored;
                break;
            case TileVisibilityState.Explored:
                if (tileHighlightGameObject != null) tileVisibleStateGameObject.GetComponent<SpriteRenderer>().color = ColorManager.Instance.tile_Explored;
                break;
            case TileVisibilityState.Visible:
                if (tileHighlightGameObject != null) tileVisibleStateGameObject.GetComponent<SpriteRenderer>().color = Color.clear;
                break;
        }
    }

    public enum TileVisibilityState
    {
        Unexplored, //Black
        Explored,   //When player hav visit it once.
        Visible, //Player can se.
    }
}
