using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GravityWithSlide : MonoBehaviour
{
    public BoardState board;
    public TopFiller filler;

    [Header("Anim")]
    public float moveDurPerCell = 0.07f;
    public float staggerDelay = 0.02f;

    [Header("Hex Gravity Settings")]
    public float verticalThreshold = 0.3f; // x-정렬 허용치

    // ---------- Core helpers ----------

    private bool IsBottomRow(Vector3Int cell)
    {
        var all = board.AllCells().ToList();
        if (all.Count == 0) return false;
        float minY = all.Min(c => board.WorldCenter(c).y);
        float y = board.WorldCenter(cell).y;
        return y <= minY + 0.1f;
    }

    // 더 낮은 빈칸이 하나라도 있으면 "불안정"(= 더 내려가야 함)
    private bool HasDownwardEmpty(Vector3Int cell)
    {
        var cw = board.WorldCenter(cell);
        for (int i = 0; i < 6; i++)
        {
            var n = PuzzleDirs.Step(cell, i);
            if (!board.IsValidCell(n) || !board.IsEmpty(n)) continue;
            var nw = board.WorldCenter(n);
            if (nw.y < cw.y - 0.01f) return true;
        }
        return false;
    }

    private bool IsStable(Vector3Int cell)
    {
        return !HasDownwardEmpty(cell);
    }

    // 낙하 목적지(수직 우선, 없으면 슬라이드) - 끝까지 반복
    private Vector3Int FindFallDestinationWithSlide(Vector3Int start)
    {
        var current = start;
        var cw = board.WorldCenter(current);
        int guard = 0;

        while (guard++ < 128)
        {
            Vector3Int best = current;
            float bestScore = float.PositiveInfinity;

            for (int i = 0; i < 6; i++)
            {
                var n = PuzzleDirs.Step(current, i);
                if (!board.IsValidCell(n) || !board.IsEmpty(n)) continue;

                var nw = board.WorldCenter(n);
                float dy = cw.y - nw.y;
                if (dy <= 0.01f) continue; // 아래쪽만

                float dx = Mathf.Abs(cw.x - nw.x);

                // 수직 우선: 같은 열이면 큰 보너스(낮은 점수)
                float verticalBonus = dx < verticalThreshold ? -50f : 0f;

                // 더 낮을수록(= y가 작을수록) 우선, 수직에 가까울수록 우선
                float score = nw.y * 100f + dx * 10f + verticalBonus;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = n;
                }
            }

            if (best == current) break;

            current = best;
            cw = board.WorldCenter(current);
        }

        return current;
    }

    // (3,0)과 같은 '입구 열'의 최상단 빈칸 찾기
    private Vector3Int FindTopEntryAbove(Vector3Int entryBase)
    {
        float ex = board.WorldCenter(entryBase).x;

        var column = board.AllCells()
            .Where(c => Mathf.Abs(board.WorldCenter(c).x - ex) < verticalThreshold * 0.9f)
            .OrderByDescending(c => board.WorldCenter(c).y);

        foreach (var c in column)
            if (board.IsEmpty(c)) return c;

        return entryBase;
    }

    // ---------- Public flows ----------

    // 처음 채우기: (3,0) 입구로 하나 넣고 → Collapse로 ‘흘러내리기’를 30번 반복
    public IEnumerator FillInitialBoard(int totalPieces)
    {
        for (int i = 0; i < totalPieces; i++)
        {
            yield return SpawnFromTopEntry();   // ⬅️ 목표 셀 직행 대신, 입구에 등록
            yield return CollapseAnimated();    // ⬅️ 수직+슬라이드로 자연 낙하
        }
    }

    // 매치 후 스폰도 동일 규칙으로
    public IEnumerator ApplyWithSpawn(int spawnCount)
    {
        yield return CollapseAnimated();

        for (int i = 0; i < spawnCount; i++)
        {
            yield return SpawnFromTopEntry();
            // 필요하면 약간의 텀: yield return new WaitForSeconds(0.03f);
        }

        yield return CollapseAnimated();
    }

    // (핵심) (3,0) 위에서 등장 → (3,0) 열 최상단 빈칸에 '등록' → 자연 낙하
    private IEnumerator SpawnFromTopEntry()
    {
        Vector3Int entryBase = new Vector3Int(3, 0, 0);
        Vector3Int entry = FindTopEntryAbove(entryBase);
        if (!board.IsEmpty(entry)) yield break;

        int type = Random.Range(0, Mathf.Min(filler.colorCount, filler.typeSprites.Length));
        var go = Instantiate(filler.piecePrefab);
        var pz = go.GetComponent<Puzzle>();
        pz.SetType(type, filler.typeSprites[type]);

        Vector3 spawnTop = board.WorldCenter(entryBase) + Vector3.up * filler.spawnHeightOffset;
        go.transform.position = spawnTop;

        // 입구 셀에 등록(중요)
        board.pieces[entry] = go;

        // 입구까지 짧게 이동
        yield return MoveTo(go.transform, board.WorldCenter(entry), 0.12f);

        // 나머지는 자연 낙하
        yield return CollapseAnimated();
    }

    // ---------- Gravity pass ----------

    // 보드 안정화: 아래로 갈 곳이 있는 조각들을 모두 목적지까지 이동
    public IEnumerator CollapseAnimated()
    {
        bool moved;
        int safety = 0;

        do
        {
            moved = false;
            safety++;

            // 아래부터(작은 y) 처리 → 충돌 최소화
            var candidates = board.pieces
                .Where(kv => !IsStable(kv.Key))
                .OrderBy(kv => board.WorldCenter(kv.Key).y)
                .ToList();

            var moves = new List<(Transform tr, Vector3Int from, Vector3Int to)>();

            foreach (var kv in candidates)
            {
                var from = kv.Key;
                var go = kv.Value;

                if (!board.pieces.ContainsKey(from)) continue;

                var dest = FindFallDestinationWithSlide(from);
                if (dest == from) continue;

                moved = true;

                board.pieces.Remove(from);
                board.pieces[dest] = go;

                moves.Add((go.transform, from, dest));
            }

            // 애니메이션 실행(약간의 계단식 지연)
            if (moves.Count > 0)
            {
                var coroutines = new List<Coroutine>();
                for (int i = 0; i < moves.Count; i++)
                {
                    var (tr, _, to) = moves[i];
                    float delay = i * staggerDelay;
                    coroutines.Add(StartCoroutine(MoveWithDelay(tr, board.WorldCenter(to), delay)));
                }
                foreach (var c in coroutines) yield return c;
            }

        } while (moved && safety < 20);
    }

    private IEnumerator MoveWithDelay(Transform tr, Vector3 target, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        yield return MoveTo(tr, target, moveDurPerCell);
    }

    private IEnumerator MoveTo(Transform tr, Vector3 to, float dur)
    {
        Vector3 from = tr.position;
        float t = 0f;

        while (t < dur)
        {
            float u = Mathf.Clamp01(t / Mathf.Max(0.0001f, dur));
            // 가속 느낌 (중력감)
            float eased = u * u;
            tr.position = Vector3.LerpUnclamped(from, to, eased);
            t += Time.deltaTime;
            yield return null;
        }
        tr.position = to;
    }

    // ---------- (선택) 이전 보조 로직: 남겨두고 싶으면 아래 유지 ----------

    private List<Vector3Int> GetDirectSupportNeighbors(Vector3Int cell)
    {
        var result = new List<Vector3Int>();
        var cw = board.WorldCenter(cell);

        for (int i = 0; i < 6; i++)
        {
            var n = PuzzleDirs.Step(cell, i);
            if (!board.IsValidCell(n)) continue;

            var nw = board.WorldCenter(n);
            var yDiff = cw.y - nw.y;
            var xDiff = Mathf.Abs(cw.x - nw.x);

            if (yDiff > 0.1f && xDiff < verticalThreshold)
                result.Add(n);
        }
        return result;
    }
}
