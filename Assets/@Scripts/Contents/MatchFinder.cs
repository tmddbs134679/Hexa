using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MatchFinder : MonoBehaviour
{
    [SerializeField] private BoardState _board;

    /// 보드 전체에서 라인(>=3) + 군집(>=4) 매치 수집
    public HashSet<Vector3Int> CollectAllMatches()
    {
        var keys = _board.pieces.Keys.ToList();

        var all = new HashSet<Vector3Int>();

        // 1) 라인 매치
        var lines = CollectAllLineMatches(keys);
        all.UnionWith(lines);

        // 2) 군집 매치(마름모 포함하는 덩어리만)
        var clusters = CollectAllClusterMatches(keys);
        all.UnionWith(clusters);

        return all;
    }


    // Line Matches (3축 직선)
    HashSet<Vector3Int> CollectAllLineMatches(List<Vector3Int> keys)
    {
        var result = new HashSet<Vector3Int>();

        foreach (var s in keys)
        {
            if (!_board.pieces.TryGetValue(s, out var go)) continue;
            var pz = go.GetComponent<Puzzle>();
            if (pz == null || !pz.IsMatchable) continue;

            int type = pz.typeId;

            // 3개 축(E-W, NE-SW, NW-SE)
            foreach (var axis in PuzzleDirs.AXES)
            {
                // 앵커 판정: -방향 이웃이 같은 타입(매치 가능)이면 앵커 아님
                var prev = PuzzleDirs.Step(s, axis[1]);
                if (IsSameTypeForMatch(prev, type)) continue;

                // +방향 러닝
                var run = new List<Vector3Int> { s };
                var cur = s;
                while (true)
                {
                    var n = PuzzleDirs.Step(cur, axis[0]);
                    if (!IsSameTypeForMatch(n, type)) break;
                    run.Add(n);
                    cur = n;
                }

                if (run.Count >= Define.MIN_LINE)
                    foreach (var c in run) result.Add(c);
            }
        }

        return result;
    }

  
    // Cluster Matches (마름모 포함 컴포넌트만)
    HashSet<Vector3Int> CollectAllClusterMatches(List<Vector3Int> keys)
    {
        var result = new HashSet<Vector3Int>();
        var visited = new HashSet<Vector3Int>();

        foreach (var s in keys)
        {
            if (visited.Contains(s)) continue;
            if (!_board.pieces.TryGetValue(s, out var go)) continue;

            var pz = go.GetComponent<Puzzle>();
            if (pz == null || !pz.IsMatchable) continue;

            int type = pz.typeId;
            var comp = FloodFillSameType(s, type, visited); // 매치 가능한 것만 채움
            if (comp.Count < Define.MIN_CLUSTER) continue;

            if (ContainsAnyRhombus(comp))
                foreach (var c in comp) result.Add(c);
        }

        return result;
    }

    // 군집 판정: 2×2 마름모(평행사변형) 포함 여부 
    bool ContainsAnyRhombus(HashSet<Vector3Int> comp)
    {
        // comp 안의 각 셀 a에 대해, 인접한 두 방향(dirA, dirB) 조합으로
        // a, a+dirA, a+dirB, a+dirA+dirB 가 모두 있으면 마름모 성립
        foreach (var a in comp)
        {
            for (int i = 0; i < 6; i++)
            {
                int left = (i + 5) % 6; // i의 왼쪽 인접
                int right = (i + 1) % 6; // i의 오른쪽 인접

                if (HasRhombusAt(a, i, left, comp)) return true;
                if (HasRhombusAt(a, i, right, comp)) return true;
            }
        }
        return false;
    }

    bool HasRhombusAt(Vector3Int a, int dirA, int dirB, HashSet<Vector3Int> comp)
    {
        var b = PuzzleDirs.Step(a, dirA); // a + dirA
        var c = PuzzleDirs.Step(a, dirB); // a + dirB
        var d = PuzzleDirs.Step(b, dirB); // a + dirA + dirB (평행사변형의 4번째 꼭짓점)

        return comp.Contains(b) && comp.Contains(c) && comp.Contains(d);
    }

  
    // Flood Fill (같은 type + 매치 가능만)
    HashSet<Vector3Int> FloodFillSameType(Vector3Int start, int type, HashSet<Vector3Int> visited)
    {
        var comp = new HashSet<Vector3Int>();
        var q = new Queue<Vector3Int>();

        if (!IsSameTypeForMatch(start, type)) return comp;

        visited.Add(start);
        q.Enqueue(start);

        while (q.Count > 0)
        {
            var c = q.Dequeue();
            comp.Add(c);

            for (int i = 0; i < 6; i++)
            {
                var n = PuzzleDirs.Step(c, i);
                if (!visited.Contains(n) && IsSameTypeForMatch(n, type))
                {
                    visited.Add(n);
                    q.Enqueue(n);
                }
            }
        }
        return comp;
    }

    //  Helper: 매치 가능한 퍼즐"만 같은 타입으로 인정 
    bool IsSameTypeForMatch(Vector3Int c, int type)
    {
        if (!_board.pieces.TryGetValue(c, out var go))
            return false;

        var pz = go.GetComponent<Puzzle>();
        return pz != null && pz.IsMatchable && pz.typeId == type;
    }
}
