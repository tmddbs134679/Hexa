using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MatchFinder : MonoBehaviour
{
    public BoardState board;
    public const int MIN_LINE = 3;
    public const int MIN_CLUSTER = 4;

    public HashSet<Vector3Int> CollectAllMatches()
    {
        var result = new HashSet<Vector3Int>();

        // 1) 직선 ≥3
        foreach (var s in board.pieces.Keys)
        {
            var go = board.pieces[s];
            int type = go.GetComponent<Puzzle>().typeId;

            foreach (var axis in PuzzleDirs.AXES)
            {
                var line = new List<Vector3Int> { s };

                var cur = s;
                while (IsSameType(PuzzleDirs.Step(cur, axis[0]), type))
                { cur = PuzzleDirs.Step(cur, axis[0]); line.Add(cur); }
                cur = s;
                while (IsSameType(PuzzleDirs.Step(cur, axis[1]), type))
                { cur = PuzzleDirs.Step(cur, axis[1]); line.Add(cur); }

                if (line.Count >= MIN_LINE)
                    foreach (var c in line) result.Add(c);
            }
        }

        // 2) 군집 ≥4 (면 접촉 연결)
        var visited = new HashSet<Vector3Int>();
        foreach (var s in board.pieces.Keys)
        {
            if (visited.Contains(s)) continue;
            int type = board.pieces[s].GetComponent<Puzzle>().typeId;

            var comp = FloodFillSameType(s, type, visited);
            if (comp.Count >= MIN_CLUSTER)
                foreach (var c in comp) result.Add(c);
        }

        return result;
    }

    List<Vector3Int> FloodFillSameType(Vector3Int start, int type, HashSet<Vector3Int> visited)
    {
        var q = new Queue<Vector3Int>();
        var comp = new List<Vector3Int>();
        q.Enqueue(start); visited.Add(start);

        while (q.Count > 0)
        {
            var c = q.Dequeue();
            comp.Add(c);
            for (int i = 0; i < 6; i++)
            {
                var n = PuzzleDirs.Step(c, i);
                if (!visited.Contains(n) && IsSameType(n, type))
                {
                    visited.Add(n);
                    q.Enqueue(n);
                }
            }
        }
        return comp;
    }

    bool IsSameType(Vector3Int c, int type)
    {
        if (!board.pieces.TryGetValue(c, out var go)) return false;
        return go.GetComponent<Puzzle>().typeId == type;
    }
}
