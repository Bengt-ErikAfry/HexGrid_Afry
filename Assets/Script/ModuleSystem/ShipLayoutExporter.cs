

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class ShipLayoutExporter : MonoBehaviour
{
    [Header("Layout Identity")]
    public string shipId = "frigate_mk1";
    public string imagePath = "ui/ships/frigate_mk1"; // optional hint for runtime

    [Header("Scene References")]
    public RectTransform shipImage;            // The RectTransform that displays the ship sprite
    public Transform markersParent;            // Parent containing SlotMarker children anywhere under it

    [Header("Export Settings")]
    public string lastExportPath = "Assets/ShipLayouts/frigate_mk1.json";  // remembered after first export
    public bool clampPositionsToRect = true;   // If true, will clamp normalized pos to [0..1]
    public bool warnOnOutOfBounds = true;      // Warn if any marker resolves outside ship rect

    // ---------- DTOs used for JSON ----------
    [Serializable]
    public class SlotDTO
    {
        public int slotId;  //Used to determine in hanger view if first slot is setup(Hull)
        //public string moduleType; //ModuleType on the slot is determined by unit modulelist.
        public List<ModuleType> acceptedModuleTypes = new List<ModuleType>();
        public float[] pos;     // normalized [x,y] in [0..1]
    }

    [Serializable]
    public class LayoutDTO
    {
        public string shipId;
        public string imagePath;
        public SlotDTO[] slots;
    }
    // ----------------------------------------

#if UNITY_EDITOR

    [ContextMenu("Export Layout JSON...")]
    public void ExportLayoutJsonContext()
    {
        if (!ValidateBasics())
            return;

        var dto = BuildLayoutDTO(out var warnings);

        // Ask for a target file the first time or if path is empty
        string path = lastExportPath;
        string defaultName = string.IsNullOrEmpty(shipId) ? "ship_layout.json" : $"{shipId}.json";

        path = EditorUtility.SaveFilePanelInProject(
            "Export Ship Layout JSON",
            defaultName,
            "json",
            "Choose where to save the ship layout JSON.",
            string.IsNullOrEmpty(path) ? "Assets" : Path.GetDirectoryName(path));

        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("path to file empty or null");
            return;
        }

        lastExportPath = path;

        var json = JsonUtility.ToJson(dto, prettyPrint: true);
        File.WriteAllText(path, json);
        AssetDatabase.Refresh();

        if (!string.IsNullOrEmpty(warnings))
            Debug.LogWarning($"[ShipLayoutExporter] Export warnings for '{shipId}':\n{warnings}");

        Debug.Log($"[ShipLayoutExporter] Exported layout to: {path}");
    }

    [ContextMenu("Import Layout JSON...")]
    public void ImportLayoutJsonContext()
    {
        if (!ValidateBasics())
            return;

        string path = EditorUtility.OpenFilePanel("Import Ship Layout JSON", "Assets", "json");
        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("path to file empty or null");
            return;
        }

        string text = File.ReadAllText(path);
        var dto = JsonUtility.FromJson<LayoutDTO>(text);
        if (dto == null || dto.slots == null)
        {
            Debug.LogError("[ShipLayoutExporter] Failed to parse JSON.");
            return;
        }
    }

    private bool ValidateBasics()
    {
        if (shipImage == null)
        {
            Debug.LogError("[ShipLayoutExporter] Please assign shipImage (RectTransform).");
            return false;
        }
        if (markersParent == null)
        {
            Debug.LogError("[ShipLayoutExporter] Please assign markersParent.");
            return false;
        }
        return true;
    }

    private LayoutDTO BuildLayoutDTO(out string warnings)
    {
        warnings = string.Empty;
        var warnLines = new List<string>();

        var markers = markersParent.GetComponentsInChildren<ModuleView_Slot>(true);
        if (markers.Length == 0)
        {
            warnLines.Add("No SlotMarker components found under markersParent.");
        }
        /*
        // Duplicate slotId detection
        var dupGroups = markers
            .GroupBy(m => m.requiredModuleTypes)
            .Where(static g => !string.IsNullOrEmpty(g.Key) && g.Count() > 1)
            .ToList();*/
        var dupGroups = markers
            .GroupBy(m => m.acceptedModuleTypes)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var g in dupGroups)
            warnLines.Add($"Duplicate slotId '{g.Key}' used {g.Count()} times.");

        var slots = new List<SlotDTO>();

        foreach (var marker in markers)
        {
            if (!TryWorldToNormalized(marker.transform, out Vector2 normalized, out string posWarn))
            {
                warnLines.Add(posWarn);
                continue;
            }

            var x = normalized.x;
            var y = normalized.y;

            bool outOfBounds = (x < 0f || x > 1f || y < 0f || y > 1f);
            if (outOfBounds && warnOnOutOfBounds)
            {
                warnLines.Add($"Marker '{marker.acceptedModuleTypes}' resolved outside ship rect: ({x:F3},{y:F3}).");
            }

            if (clampPositionsToRect)
            {
                x = Mathf.Clamp01(x);
                y = Mathf.Clamp01(y);
            }

            slots.Add(new SlotDTO
            {
                slotId = marker.slotIndex,
                acceptedModuleTypes = marker.acceptedModuleTypes,
                pos = new[] { x, y },
            });
        }

        if (warnLines.Count > 0)
            warnings = string.Join("\n", warnLines);

        return new LayoutDTO
        {
            shipId = shipId,
            imagePath = imagePath,
            slots = slots
            .OrderBy(s => s.slotId)
            .ToArray()
                        //slots = slots.OrderBy(s => s.moduleType).ThenBy(s => s.slotId).ToArray()
        };
    }

    /// <summary>
    /// Converts the world position of a marker Transform into normalized [0..1] coords within shipImage rect.
    /// Works across different canvas render modes and hierarchies.
    /// </summary>
    private bool TryWorldToNormalized(Transform markerTr, out Vector2 normalized, out string warn)
    {
        warn = string.Empty;
        normalized = Vector2.zero;

        var markerRT = markerTr as RectTransform;
        Vector3 worldPos = markerTr.position;

        // Find the canvas and its camera (if any)
        var canvas = shipImage.GetComponentInParent<Canvas>();
        Camera cam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        // Convert marker world pos -> screen pos, then -> shipImage local point
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(shipImage, screen, cam, out localPoint))
        {
            warn = $"Failed to convert position for marker '{markerTr.name}'.";
            return false;
        }

        // Now we have localPoint in the shipImage rect local space (pivot-based coordinates)
        Rect r = shipImage.rect;
        float nx = Mathf.InverseLerp(r.xMin, r.xMax, localPoint.x);
        float ny = Mathf.InverseLerp(r.yMin, r.yMax, localPoint.y);

        normalized = new Vector2(nx, ny);
        return true;
    }

    /// <summary>
    /// Reverse of TryWorldToNormalized: normalized -> world position on shipImage plane.
    /// Useful for Import.
    /// </summary>
    private bool TryNormalizedToWorldInShipImage(float nx, float ny, out Vector3 world)
    {
        world = Vector3.zero;

        Rect r = shipImage.rect;
        float lx = Mathf.Lerp(r.xMin, r.xMax, nx);
        float ly = Mathf.Lerp(r.yMin, r.yMax, ny);
        Vector3 local = new Vector3(lx, ly, 0f);

        // Convert local point in shipImage to world
        world = shipImage.TransformPoint(local);
        return true;
    }

#endif // UNITY_EDITOR
}

