using UnityEngine;

// Place a Door GameObject between two tiles (or attach to one tile) and set tileA/tileB to the two adjacent tile coords.
// The door can be opened/closed; closed doors block movement between the two tiles.
public class DoorComponent : MonoBehaviour
{
    [Tooltip("Tile coordinate A (col=x, row=y)")]
    public Vector2Int tileA;
    [Tooltip("Tile coordinate B (col=x, row=y)")]
    public Vector2Int tileB;

    [Tooltip("If false, the door blocks movement.")]
    public bool isOpen = true;

    // Editor helper: set coords from nearby Tile prefab (optional)
    public void Toggle() => isOpen = !isOpen;

    public bool Connects(Vector2Int a, Vector2Int b)
    {
        // unordered compare
        return (tileA == a && tileB == b) || (tileA == b && tileB == a);
    }
}