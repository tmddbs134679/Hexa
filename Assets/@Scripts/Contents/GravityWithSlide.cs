using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GravityWithSlide : MonoBehaviour
{
    public BoardState board;
    public TopFiller filler;

    [Header("Anim")]
    public float moveDurPerCell = 0.07f;   // 한 칸 떨어질 때 연출 속도

    // 매치 제거 후: 기존 조각들 낙하 → 빈칸 수만큼 스폰 → 다시 낙하
    public IEnumerator ApplyWithSpawn(int spawnCount)
    {
        // 1) 현재 조각 낙하
        yield return CollapseAnimated();

        // 2) 스폰 (빈칸 수만큼)
        var empties = board.EmptyCells()
                           .OrderBy(c => board.WorldCenter(c).y) // <- 아래부터
                           .Take(spawnCount)
                           .ToList();

        yield return filler.SpawnIntoMany(empties, 0.12f, 0.01f);

        // 3) 스폰 후 한 번 더 낙하(슬라이드 포함)
        yield return CollapseAnimated();
    }

    // 보드가 안정될 때까지 반복 낙하 (한 번에 최종 목적지까지)
    public IEnumerator CollapseAnimated()
    {
        bool moved;
        int safety = 0;

        do
        {
            moved = false;
            safety++;

            // 아래(월드 y가 작은) -> 위 순으로 처리
            var snapshot = board.pieces.ToList()
                                .OrderBy(kv => board.WorldCenter(kv.Key).y)
                                .ToList();

            foreach (var kv in snapshot)
            {
                var fromCell = kv.Key;
                var go = kv.Value;

                if (!board.pieces.ContainsKey(fromCell)) continue; // 이미 이동/제거됨
                var dest = FindFallDestination(fromCell);
                if (dest == fromCell) continue;

                moved = true;
                // 사전 업데이트
                board.pieces.Remove(fromCell);
                board.pieces[dest] = go;

                // 애니메이션: 경로 길이에 비례
                float cells = Mathf.Max(1, Mathf.RoundToInt((board.WorldCenter(fromCell) - board.WorldCenter(dest)).magnitude));
                yield return MoveTo(go.transform, board.WorldCenter(dest), moveDurPerCell * cells);
            }

        } while (moved && safety < 32);
    }

    // 목적지 탐색: "월드 Y가 더 낮은(아래쪽) 빈 이웃"으로만 이동 → 무한루프 없음
    Vector3Int FindFallDestination(Vector3Int start)
    {
        var cur = start;
        int guard = 0;
        while (guard++ < 128)
        {
            var next = ChooseDownNeighbor(cur);
            if (next == cur) break;               // 더 내려갈 곳 없음
            cur = next;
        }
        return cur;
    }



    // 아래쪽으로 이동 가능한 이웃 중 하나를 선택(우선순위: 더 아래인 것 → x 차이 적은 것)
    Vector3Int ChooseDownNeighbor(Vector3Int c)
    {
        var pos = board.WorldCenter(c);
        // 6이웃 후보 중 "아래에 있고" "비어있는" 칸만
        var candidates = new List<Vector3Int>();
        for (int i = 0; i < 6; i++)
        {
            var n = PuzzleDirs.Step(c, i);
            if (!board.IsValidCell(n) || !board.IsEmpty(n)) continue;

            var wn = board.WorldCenter(n);
            if (wn.y < pos.y - 1e-3f) candidates.Add(n); // y가 더 낮아야 '하강'
        }

        if (candidates.Count == 0) return c;

        // 더 아래(y가 작은) 순, 그 다음 x 차이가 작은 순(직하 우선 느낌)
        candidates = candidates
            .OrderBy(n => board.WorldCenter(n).y)
            .ThenBy(n => Mathf.Abs(board.WorldCenter(n).x - pos.x))
            .ToList();

        return candidates[0];
    }

    IEnumerator MoveTo(Transform tr, Vector3 to, float dur)
    {
        Vector3 a = tr.position; float t = 0f;
        while (t < dur)
        {
            float u = t / Mathf.Max(dur, 0.0001f);
            tr.position = Vector3.LerpUnclamped(a, to, u);
            t += Time.deltaTime; yield return null;
        }
        tr.position = to;
    }
}
