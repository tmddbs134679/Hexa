using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 라인(>=3) + 군집(>=4, 사슬 제외) 매치 탐색기
/// - 라인 매치: 3축 직선 러닝카운트
/// - 군집 매치: FloodFill 후 (사이클 존재 || 차수>=3) 인 컴포넌트만 인정
/// </summary>
public class MatchFinder : MonoBehaviour
{
    public BoardState board;

    [Header("Line Rule")]
    public bool enableLineMatches = true;
    public int minLineLen = 3;

    [Header("Cluster Rule")]
    public bool enableClusterMatches = true;
    public int minClusterSize = 4;
    // 군집 인정 조건: (사이클 존재 || 분기(차수>=3))
    public bool clusterNeedsCycleOrBranch = true;

    // ---------------------------
    // Public API
    // ---------------------------

    /// <summary>
    /// 보드 전체에서 라인/군집 매치 수집 (합집합)
    /// </summary>
    public HashSet<Vector3Int> CollectAllMatches()
    {
        var keys = board.pieces.Keys.ToList();

        var allMatches = new HashSet<Vector3Int>();

        if (enableLineMatches)
        {
            var lineMatches = CollectAllLineMatches(keys);
            allMatches.UnionWith(lineMatches);
        }

        if (enableClusterMatches)
        {
            var clusterMatches = CollectAllClusterMatches(keys);
            allMatches.UnionWith(clusterMatches);
        }

        return allMatches;
    }

    // ---------------------------
    // Line Matches
    // ---------------------------

    HashSet<Vector3Int> CollectAllLineMatches(List<Vector3Int> keys)
    {
        var result = new HashSet<Vector3Int>();

        foreach (var s in keys)
        {
            if (!board.pieces.TryGetValue(s, out var go)) continue;
            int type = go.GetComponent<Puzzle>().typeId;

            // 3개 축(E-W, NE-SW, NW-SE)
            foreach (var axis in PuzzleDirs.AXES)
            {
                // 앵커 판정: -방향 이웃이 같은 타입이면 앵커 아님
                var prev = PuzzleDirs.Step(s, axis[1]);
                if (IsSameType(prev, type)) continue;

                // +방향으로 러닝카운트
                var run = new List<Vector3Int> { s };
                var cur = s;
                while (true)
                {
                    var n = PuzzleDirs.Step(cur, axis[0]);
                    if (!IsSameType(n, type)) break;

                    run.Add(n);
                    cur = n;
                }

                if (run.Count >= minLineLen)
                    foreach (var c in run) result.Add(c);
            }
        }

        return result;
    }

    // ---------------------------
    // Cluster Matches (FloodFill -> 사이클/분기 검사)
    // ---------------------------

    HashSet<Vector3Int> CollectAllClusterMatches(List<Vector3Int> keys)
    {
        var result = new HashSet<Vector3Int>();
        var visited = new HashSet<Vector3Int>();

        foreach (var s in keys)
        {
            if (visited.Contains(s)) continue;
            if (!board.pieces.TryGetValue(s, out var go)) continue;

            int type = go.GetComponent<Puzzle>().typeId;

            var comp = FloodFillSameType(s, type, visited);
            if (comp.Count < minClusterSize) continue;

            if (!clusterNeedsCycleOrBranch || IsClusterComponent(comp))
            {
                foreach (var c in comp) result.Add(c);
            }
        }

        return result;
    }
    bool IsClusterComponent(HashSet<Vector3Int> comp)
    {
        if (comp.Count < minClusterSize) return false; // 보통 4

        foreach (var a in comp)
        {
            // 6방향 중 인접한 두 방향 쌍만 검사 (i와 i±1)
            for (int i = 0; i < 6; i++)
            {
                int left = (i + 5) % 6; // i의 왼쪽 인접
                int right = (i + 1) % 6; // i의 오른쪽 인접

                if (HasRhombusAt(a, i, left, comp)) return true;
                if (HasRhombusAt(a, i, right, comp)) return true;
            }
        }
        return false; // 마름모가 하나도 없으면 군집 아님
    }

    bool HasRhombusAt(Vector3Int a, int dirA, int dirB, HashSet<Vector3Int> comp)
    {
        var b = PuzzleDirs.Step(a, dirA);       // a + dirA
        var c = PuzzleDirs.Step(a, dirB);       // a + dirB
        var d = PuzzleDirs.Step(b, dirB);       // a + dirA + dirB  (평행사변형의 4번째 꼭짓점)

        return comp.Contains(b) && comp.Contains(c) && comp.Contains(d);
    }

    // ---------------------------
    // Flood Fill (같은 type 연결 컴포넌트)
    // ---------------------------

    HashSet<Vector3Int> FloodFillSameType(Vector3Int start, int type, HashSet<Vector3Int> globalVisited)
    {
        var comp = new HashSet<Vector3Int>();
        var q = new Queue<Vector3Int>();

        if (!IsSameType(start, type)) return comp;

        globalVisited.Add(start);
        q.Enqueue(start);

        while (q.Count > 0)
        {
            var c = q.Dequeue();
            comp.Add(c);

            for (int i = 0; i < 6; i++)
            {
                var n = PuzzleDirs.Step(c, i);
                if (!globalVisited.Contains(n) && IsSameType(n, type))
                {
                    globalVisited.Add(n);
                    q.Enqueue(n);
                }
            }
        }

        return comp;
    }

    // ---------------------------
    // Helpers
    // ---------------------------

    bool IsSameType(Vector3Int c, int type)
        => board.pieces.TryGetValue(c, out var go) && go.GetComponent<Puzzle>().typeId == type;
}
