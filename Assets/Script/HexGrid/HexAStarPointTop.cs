
using System.Collections.Generic;
using UnityEngine;

public static class HexAStarPointTop
{
    // Point-top neighbor offsets
    private static readonly Vector2Int[] Neighbors =
    {
        new(1, 0), new(1, -1), new(0, -1),
        new(-1, 0), new(-1, 1), new(0, 1)
    };

    public static List<Vector2Int> FindPath(
        Vector2Int start, Vector2Int goal,
        int worldRadius,
        HashSet<Vector2Int> blocked // can be null
    )
    {
        // Early outs
        if (!InsideHexRadius(start, worldRadius) || !InsideHexRadius(goal, worldRadius))
            return null;
        if (blocked != null && blocked.Contains(start)) return null;
        if (blocked != null && blocked.Contains(goal)) return null;
        if (start == goal) return new List<Vector2Int> { start };

        // A* structures
        var open = new MinHeap();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, int> { [start] = 0 };

        int h0 = Heuristic(start, goal);
        open.Push(start, h0, 0); // f = g + h; g=0

        while (open.Count > 0)
        {
            var current = open.Pop();

            if (current == goal)
                return Reconstruct(cameFrom, start, goal);

            int gCur = gScore[current];

            for (int i = 0; i < 6; i++)
            {
                var next = current + Neighbors[i];
                if (!InsideHexRadius(next, worldRadius)) continue;
                if (blocked != null && blocked.Contains(next)) continue;

                int tentative = gCur + 1; // uniform cost movement
                if (!gScore.TryGetValue(next, out int oldG) || tentative < oldG)
                {
                    gScore[next] = tentative;
                    cameFrom[next] = current;
                    int f = tentative + Heuristic(next, goal);
                    open.Push(next, f, tentative); // tie-breaker = g
                }
            }
        }

        return null; // no route
    }

    private static int Heuristic(Vector2Int a, Vector2Int b)
    {
        // Cube distance
        int x1 = a.x, z1 = a.y, y1 = -x1 - z1;
        int x2 = b.x, z2 = b.y, y2 = -x2 - z2;
        int dx = Mathf.Abs(x1 - x2);
        int dy = Mathf.Abs(y1 - y2);
        int dz = Mathf.Abs(z1 - z2);
        return Mathf.Max(dx, dy, dz);
    }

    private static bool InsideHexRadius(Vector2Int a, int R)
    {
        int x = a.x, z = a.y, y = -x - z;
        return Mathf.Max(Mathf.Abs(x), Mathf.Abs(y), Mathf.Abs(z)) <= R;
    }

    private static List<Vector2Int> Reconstruct(
        Dictionary<Vector2Int, Vector2Int> cameFrom,
        Vector2Int start, Vector2Int goal)
    {
        var path = new List<Vector2Int>();
        var cur = goal;
        path.Add(cur);
        while (cur != start)
        {
            cur = cameFrom[cur];
            path.Add(cur);
        }
        path.Reverse();
        return path;
    }

    // Tiny int-priority min-heap for (f, tie, node)
    private class MinHeap
    {
        private readonly List<(int f, int tie, Vector2Int n)> _a = new();
        public int Count => _a.Count;

        public void Push(Vector2Int n, int f, int tie)
        {
            _a.Add((f, tie, n));
            Up(_a.Count - 1);
        }

        public Vector2Int Pop()
        {
            var top = _a[0].n;
            int last = _a.Count - 1;
            _a[0] = _a[last];
            _a.RemoveAt(last);
            Down(0);
            return top;
        }

        private static bool Less((int f, int tie, Vector2Int n) a, (int f, int tie, Vector2Int n) b)
        {
            if (a.f != b.f) return a.f < b.f;
            return a.tie < b.tie; // tie-break by lower g
        }

        private void Up(int i)
        {
            while (i > 0)
            {
                int p = (i - 1) >> 1;
                if (Less(_a[p], _a[i])) break;
                (_a[p], _a[i]) = (_a[i], _a[p]);
                i = p;
            }
        }

        private void Down(int i)
        {
            for (; ; )
            {
                int l = i * 2 + 1, r = l + 1, s = i;
                if (l < _a.Count && Less(_a[l], _a[s])) s = l;
                if (r < _a.Count && Less(_a[r], _a[s])) s = r;
                if (s == i) return;
                (_a[i], _a[s]) = (_a[s], _a[i]);
                i = s;
            }
        }
    }
}
