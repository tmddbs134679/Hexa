using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class MatchGameLoop : MonoBehaviour
{
    [Header("Refs")]
    public BoardState board;
    public TopFiller filler;
    public GravityWithSlide gravity;
    public MatchFinder matcher;

    [Header("Init")]
    public int initialCount = 30;

    void Start()
    {
        StartCoroutine(GameStartRoutine());
    }

    IEnumerator GameStartRoutine()
    {
        // �ʱ� 30ĭ: �Ʒ�->��, ��->�� ������ ä��
        var fillOrder = board.AllCells()
                            .OrderBy(c => c.y)
                            .ThenBy(c => c.x)
                            .Take(initialCount)
                            .ToList();
        filler.FillCells(fillOrder);
        // �ʱ� �ؼ�
        yield return ResolveAllMatchesThenIdle();
    }

    public IEnumerator ResolveAllMatchesThenIdle()
    {
        while (true)
        {
            var matched = matcher.CollectAllMatches();
            if (matched.Count == 0) yield break;

            // ���� ����
            foreach (var c in matched)
            {
                if (board.pieces.TryGetValue(c, out var go))
                {
                    Destroy(go);
                    board.pieces.Remove(c);
                }
            }

            // ���� ������ŭ ��� ���� + ���� ���� ó��
            yield return StartCoroutine(gravity.ApplyWithSpawn(matched.Count));
        }
    }
}
