using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Pure hex math helpers (point-top axial). No GameObject state here.
public static class HexMath
{
    // ---------- Constants ----------
    public static readonly Vector2Int[] NeighborsPointTop =
    {
        new(1, 0), new(1, -1), new(0, -1),
        new(-1, 0), new(-1, 1), new(0, 1)
    };

    public static readonly Vector2Int[] NeighborsFlat =
    {
        new(1, 0), new(0, -1), new(-1, -1),
        new(-1, 0), new(0, 1), new(1, 1)
    };

    // ---------- Conversions ----------
    public static Vector2 AxialToWorldCenter_PointTop(Vector2Int a, float size)
    {
        float x = size * (Mathf.Sqrt(3f) * a.x + (Mathf.Sqrt(3f) / 2f) * a.y);
        float y = size * (1.5f * a.y);
        return new Vector2(x, y);
    }

    public static Vector2Int WorldToAxial_PointTop(Vector2 world, float size)
    {
        // From redblobgames point-top conversions:
        // q = (sqrt(3)/3 * x - 1/3 * y) / size
        // r = (2/3 * y) / size
        float qf = (Mathf.Sqrt(3f) / 3f * world.x - 1f / 3f * world.y) / size;
        float rf = (2f / 3f * world.y) / size;
        return CubeRound(qf, -qf - rf, rf); // cube (x,y,z) -> axial (q, r)
    }

    public static Vector2Int GetGridPosFromWorldPos(Vector3 worldPos, float size)
    {
        Vector2 worldV2 = new Vector2(worldPos.x, worldPos.y);
        return WorldToAxial_PointTop(worldV2, size);
    }

    // ---------- Rounding ----------
    public static Vector2Int CubeRound(float x, float y, float z)
    {
        int rx = Mathf.RoundToInt(x), ry = Mathf.RoundToInt(y), rz = Mathf.RoundToInt(z);
        float dx = Mathf.Abs(rx - x), dy = Mathf.Abs(ry - y), dz = Mathf.Abs(rz - z);

        if (dx > dy && dx > dz) rx = -ry - rz;
        else if (dy > dz) ry = -rx - rz;
        else rz = -rx - ry;

        return new Vector2Int(rx, rz); // axial (q=rx, r=rz)
    }

    // ---------- Spatial tests & ranges ----------

    // Return number of cells in a hex of radius R
    public static int CellsInRadius(int R) => 1 + 3 * R * (R + 1);

    // Smallest radius R such that number of cells >= desiredCount
    public static int MinRadiusForCount(int desiredCount)
    {
        if (desiredCount <= 1) return 0;
        int r = 0;
        while (CellsInRadius(r) < desiredCount)
        {
            r++;
            if (r > 10000) break; // safety
        }
        return r;
    }

    // Axial region inside radius R (unsorted)
    public static List<Vector2Int> AxialRegion(int R)
    {
        var list = new List<Vector2Int>();
        for (int r = -R; r <= R; r++)
            for (int q = -R; q <= R; q++)
            {
                int x = q, z = r, y = -x - z;
                if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y), Mathf.Abs(z)) <= R)
                    list.Add(new Vector2Int(q, r));
            }
        return list;
    }

    // IEnumerable helper used by existing callers
    public static IEnumerable<Vector2Int> AxialInsideHex(int R)
    {
        for (int r = -R; r <= R; r++)
            for (int q = -R; q <= R; q++)
            {
                int x = q, z = r, y = -x - z;
                if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y), Mathf.Abs(z)) <= R)
                    yield return new Vector2Int(q, r);
            }
    }

    // Canonical generator used by UI / generator / baker
    public static List<Vector2Int> GenerateCellsForCountSorted(int desiredCount)
    {
        int R = MinRadiusForCount(desiredCount);
        var cells = AxialRegion(R);
        cells.Sort((a, b) =>
        {
            int da = Mathf.Max(Mathf.Abs(a.x), Mathf.Abs(a.y), Mathf.Abs(-a.x - a.y));
            int db = Mathf.Max(Mathf.Abs(b.x), Mathf.Abs(b.y), Mathf.Abs(-b.x - b.y));
            if (da != db) return da.CompareTo(db);
            if (a.x != b.x) return a.x.CompareTo(b.x);
            return a.y.CompareTo(b.y);
        });
        if (cells.Count > desiredCount) cells.RemoveRange(desiredCount, cells.Count - desiredCount);
        return cells;
    }

    // Convenience neighbor dirs accessor
    public static Vector2Int[] NeighborDirs() => NeighborsPointTop; // project uses point-top

    // Compatibility helpers requested
    public static bool InsideHex(Vector2Int axial, int R)
    {
        int x = axial.x, z = axial.y, y = -x - z;
        return Mathf.Max(Mathf.Abs(x), Mathf.Abs(y), Mathf.Abs(z)) <= R;
    }

    // Alias for callers expecting the name InsideHexRadius
    public static bool InsideHexRadius(Vector2Int axial, int R) => InsideHex(axial, R);

    public static bool IsLexSmaller(Vector2Int a, Vector2Int b)
        => (a.x < b.x) || (a.x == b.x && a.y < b.y);

    public static void AddEdge(HashSet<(Vector2 a, Vector2 b)> set, Vector2 a, Vector2 b)
    {
        if ((b - a).sqrMagnitude > 1e-9f)
            set.Add((a, b));
    }

    // ---------- Utilities ----------
    public static List<Vector2Int> HexesInRangeAxial(Vector2Int center, int range, int worldRadius)
    {
        var results = new List<Vector2Int>(1 + 3 * range * (range + 1));

        int cx = center.x;
        int cz = center.y;
        int cy = -cx - cz;

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = Mathf.Max(-range, -dx - range); dy <= Mathf.Min(range, -dx + range); dy++)
            {
                int dz = -dx - dy;
                int x = cx + dx;
                int y = cy + dy;
                int z = cz + dz;
                var a = new Vector2Int(x, z);
                if (InsideHex(a, worldRadius))
                    results.Add(a);
            }
        }

        return results;
    }

    public static List<Vector2Int> HexRingAxial(Vector2Int center, int radius, int worldRadius)
    {
        var results = new List<Vector2Int>(radius == 0 ? 1 : radius * 6);
        if (radius == 0)
        {
            if (InsideHex(center, worldRadius)) results.Add(center);
            return results;
        }

        var dirs = new (int x, int y, int z)[]
        {
            ( 1,-1, 0), ( 1, 0,-1), ( 0, 1,-1),
            (-1, 1, 0), (-1, 0, 1), ( 0,-1, 1)
        };

        int cx = center.x, cz = center.y, cy = -cx - cz;
        int x = cx + dirs[4].x * radius;
        int y = cy + dirs[4].y * radius;
        int z = cz + dirs[4].z * radius;

        for (int side = 0; side < 6; side++)
        {
            var dir = dirs[side];
            for (int step = 0; step < radius; step++)
            {
                var a = new Vector2Int(x, z);
                if (InsideHex(a, worldRadius))
                    results.Add(a);
                x += dir.x; y += dir.y; z += dir.z;
            }
        }

        return results;
    }

    // ---------- New Utilities ----------
    public static int AxialDistance(Vector3 targetTo, Vector3 targetFrom, float size)
    {
        Vector2 aWorld2 = new Vector2(targetTo.x, targetTo.y);
        Vector2 bWorld2 = new Vector2(targetFrom.x, targetFrom.y);

        Vector2Int aAx = WorldToAxial_PointTop(aWorld2, size);
        Vector2Int bAx = WorldToAxial_PointTop(bWorld2, size);

        int ax = aAx.x, az = aAx.y, ay = -ax - az;
        int bx = bAx.x, bz = bAx.y, by = -bx - bz;

        int dx = Mathf.Abs(ax - bx);
        int dy = Mathf.Abs(ay - by);
        int dz = Mathf.Abs(az - bz);

        return Mathf.Max(dx, dy, dz);
    }
}