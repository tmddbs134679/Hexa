using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GravityWithSlide : MonoBehaviour
{
    public BoardState board;
    public TopFiller filler;              // ���� ��/���� ���丮 ���

    [Header("Anim")]
    public float moveDuration = 0.22f;

    /// <summary>
    /// ���� ������ŭ�� ���� ť���� ��� �����ϸ鼭
    /// �Ʒ�����ϡ����Ϸ� ĳ�����̵� ����
    /// </summary>
    public IEnumerator ApplyWithSpawn(int spawnCount)
    {
        // �̸� ���� ť�� �غ�(���ӿ�����Ʈ�� ����, board ����� ����)
        var queue = new Queue<GameObject>();
        for (int i = 0; i < spawnCount; i++)
            queue.Enqueue(filler.SpawnOne()); // ���� ���� ��ġ�� ������

        bool changed;
        do
        {
            changed = false;

            // ����Ʒ� ��ĵ: ������ �� �ִ� �ͺ��� �̵�
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

            // ���� ����: ������ ��������� �ϳ� �о�ֱ�(���� ���� ���)
            while (queue.Count > 0 && board.IsEmpty(filler.spawnCell))
            {
                var go = queue.Dequeue();
                // �������� ��ġ
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
