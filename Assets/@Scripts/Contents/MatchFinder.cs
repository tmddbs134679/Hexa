using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MatchFinder : MonoBehaviour
{
    public BoardState board;

    // ---------------------------
    // Public API
    // ---------------------------

    /// 보드 전체에서 라인(>=3) + 군집(>=4) 매치 수집
    public HashSet<Vector3Int> CollectAllMatches()
    {
        var result = new HashSet<Vector3Int>();
        var keys = board.pieces.Keys.ToList();      // 스냅샷
        var visited = new HashSet<Vector3Int>();       // 군집 방문표시

        // 1) 라인 매치(3축): "축의 음(-) 방향 이웃이 같은 타입"이면 앵커가 아니므로 스킵 -> 중복 제거
        foreach (var s in keys)
        {
            if (!board.pieces.TryGetValue(s, out var go)) continue;
            int type = go.GetComponent<Puzzle>().typeId;

            foreach (var axis in PuzzleDirs.AXES)
            {
                // axis[0] = +방향, axis[1] = -방향이라고 가정
                var prev = PuzzleDirs.Step(s, axis[1]);
                if (IsSameType(prev, type)) continue; // 중복 방지(앵커 아님)

                var line = CollectLineBoth(s, axis[0], axis[1], type);
                if (line.Count >= Define.MIN_LINE)
                    foreach (var c in line) result.Add(c);
            }
        }

        // 2) 군집 매치(≥4, 면접촉 6방)
        foreach (var s in keys)
        {
            if (visited.Contains(s)) continue;
            if (!board.pieces.TryGetValue(s, out var go)) continue;

            int type = go.GetComponent<Puzzle>().typeId;
            var comp = FloodFillSameType(s, type, visited);
            if (comp.Count >= Define.MIN_CLUSTER)
                foreach (var c in comp) result.Add(c);
        }

        return result;
    }

    /// 특정 지점들(스왑한 두 셀 등) 주변만 부분 검사
    public HashSet<Vector3Int> CollectMatchesFrom(params Vector3Int[] starts)
        => CollectMatchesFrom((IEnumerable<Vector3Int>)starts);

    public HashSet<Vector3Int> CollectMatchesFrom(IEnumerable<Vector3Int> starts)
    {
        var result = new HashSet<Vector3Int>();
        var visited = new HashSet<Vector3Int>();

        // 변경 지점 + 그 주변 6칸까지 검사 후보에 포함(긴 라인이 지나갈 수 있으므로)
        var frontier = new HashSet<Vector3Int>(starts);
        foreach (var s in starts.ToList())
            for (int i = 0; i < 6; i++)
                frontier.Add(PuzzleDirs.Step(s, i));

        // 1) 라인(중복 방지 앵커 방식)
        foreach (var s in frontier)
        {
            if (!board.pieces.TryGetValue(s, out var go)) continue;
            int type = go.GetComponent<Puzzle>().typeId;

            foreach (var axis in PuzzleDirs.AXES)
            {
                var prev = PuzzleDirs.Step(s, axis[1]);
                if (IsSameType(prev, type)) continue; // 앵커 아님

                var line = CollectLineBoth(s, axis[0], axis[1], type);
                if (line.Count >= Define.MIN_LINE)
                    foreach (var c in line) result.Add(c);
            }
        }

        // 2) 군집
        foreach (var s in frontier)
        {
            if (visited.Contains(s)) continue;
            if (!board.pieces.TryGetValue(s, out var go)) continue;

            int type = go.GetComponent<Puzzle>().typeId;
            var comp = FloodFillSameType(s, type, visited);
            if (comp.Count >= Define.MIN_CLUSTER)
                foreach (var c in comp) result.Add(c);
        }

        return result;
    }

    // ---------------------------
    // Internals
    // ---------------------------

    // s에서 시작해 +방향/−방향 모두 따라가서 같은 타입 모으기(중복 제거)
    List<Vector3Int> CollectLineBoth(Vector3Int s, int dirPos, int dirNeg, int type)
    {
        var line = new List<Vector3Int> { s };

        // +방향
        var cur = s;
        while (IsSameType(PuzzleDirs.Step(cur, dirPos), type))
        { cur = PuzzleDirs.Step(cur, dirPos); line.Add(cur); }

        // −방향
        cur = s;
        while (IsSameType(PuzzleDirs.Step(cur, dirNeg), type))
        { cur = PuzzleDirs.Step(cur, dirNeg); line.Add(cur); }

        return line;
    }

    List<Vector3Int> FloodFillSameType(Vector3Int start, int type, HashSet<Vector3Int> visited)
    {
        var q = new Queue<Vector3Int>();
        var comp = new List<Vector3Int>();

        if (!IsSameType(start, type)) return comp;

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
        => board.pieces.TryGetValue(c, out var go) && go.GetComponent<Puzzle>().typeId == type;
}
