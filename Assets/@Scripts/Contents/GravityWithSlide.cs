using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/*
    스크립트의 역할
    퍼즐 제거 후 보드에 **중력 낙하(↓ → ↘ → ↙ 우선순위)**를 적용하고, 제거된 개수만큼 즉시 스폰해서 보충하는 코루틴.
 */

public class GravityWithSlide : MonoBehaviour
{
    public BoardState board;
    public TopFiller filler;              // 스폰 셀/스폰 팩토리 사용

    [Header("Anim")]
    public float moveDuration = 0.22f;

    /// <summary>
    /// 제거 개수만큼을 스폰 큐에서 즉시 보충하면서
    /// 아래→우하→좌하로 캐스케이딩 낙하
    /// </summary>
    public IEnumerator ApplyWithSpawn(int spawnCount)
    {
        // 미리 스폰 큐를 준비(게임오브젝트만 생성, board 등록은 나중)
        var queue = new Queue<GameObject>();
        for (int i = 0; i < spawnCount; i++)
            queue.Enqueue(filler.SpawnOne()); // 스폰 월드 위치에 생성됨

        bool changed;
        do
        {
            changed = false;

            // 위→아래 스캔: 내려갈 수 있는 것부터 이동
            foreach (var kv in board.pieces.OrderByDescending(p => p.Key.y).ToList())
            {
                var from = kv.Key;
                var piece = kv.Value;
                var to = FindFallDestination(from);
                if (to == from) continue;

                changed = true;
                board.pieces.Remove(from);
                board.pieces[to] = piece;
                yield return MovePiece(piece.transform, board.WorldCenter(to), moveDuration);
            }

            // 스폰 투입: 스폰셀 비어있으면 하나 밀어넣기(연속 투입 허용)
            while (queue.Count > 0 && board.IsEmpty(filler.spawnCell))
            {
                var go = queue.Dequeue();
                // 스폰셀에 배치
                board.pieces[filler.spawnCell] = go;
                yield return MovePiece(go.transform, board.WorldCenter(filler.spawnCell), moveDuration * 0.6f);
                changed = true;
            }

        } while (changed);
    }

    Vector3Int FindFallDestination(Vector3Int cell)
    {
        var cur = cell;
        while (true)
        {
            var d = PuzzleDirs.Down(cur);
            var dr = PuzzleDirs.DownRight(cur);
            var dl = PuzzleDirs.DownLeft(cur);

            if (board.IsValidCell(d) && board.IsEmpty(d)) { cur = d; continue; }
            if (board.IsValidCell(dr) && board.IsEmpty(dr)) { cur = dr; continue; }
            if (board.IsValidCell(dl) && board.IsEmpty(dl)) { cur = dl; continue; }
            break;
        }
        return cur;
    }

    IEnumerator MovePiece(Transform t, Vector3 to, float dur)
    {
        var from = t.position; float e = 0f;
        while (e < dur)
        {
            t.position = Vector3.LerpUnclamped(from, to, e / dur);
            e += Time.deltaTime; yield return null;
        }
        t.position = to;
    }
}
