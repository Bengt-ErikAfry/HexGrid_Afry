csharp Assets/Script/HexGrid/HexGridData.cs
using UnityEngine;

[DisallowMultipleComponent]
public class HexGridData : MonoBehaviour
{
    // authoritative geometry for this grid
    public float hexSize = 1f;
    public int worldRadius = 2;

    // optional: precomputed blocked set for fast queries
    public Vector2Int[] blockedCells;

    // visual containers
    public Transform markerParent;       // where markers should be created (assign in inspector)
    public LineRenderer lineRenderer;    // optional: assign per-grid LineRenderer

    // convenience accessor
    public bool IsCellBlocked(Vector2Int axial)
    {
        if (blockedCells == null) return false;
        foreach (var b in blockedCells) if (b == axial) return true;
        return false;
    }
}