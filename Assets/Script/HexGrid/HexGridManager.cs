using System.Collections.Generic;
using UnityEngine;

// Lightweight manager: register all grids, track the active one.
public class HexGridManager : MonoBehaviour
{
    public static HexGridManager Instance { get; private set; }

    private readonly List<HexGridComponent> grids = new();
    public HexGridComponent CurrentGrid { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // Optionally DontDestroyOnLoad(gameObject);
    }

    public void Register(HexGridComponent g)
    {
        if (!grids.Contains(g)) grids.Add(g);
        if (CurrentGrid == null) CurrentGrid = g;
    }

    public void Unregister(HexGridComponent g)
    {
        grids.Remove(g);
        if (CurrentGrid == g) CurrentGrid = grids.Count > 0 ? grids[0] : null;
    }

    public void SetActiveGrid(HexGridComponent g)
    {
        if (g == null || !grids.Contains(g)) return;
        CurrentGrid = g;
        // Optionally enable visuals for CurrentGrid and disable others
        foreach (var grid in grids) grid.SetVisualActive(grid == g);
    }
}