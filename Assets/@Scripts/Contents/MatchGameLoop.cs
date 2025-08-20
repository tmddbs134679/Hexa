using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

/*
 ��ġ-3(���) ���� ������ �ڷ�ƾ���� ����: �ʱ� ��ġ �� ��ġ ���� �� ����/���� �� �ٽ� ��ġ�� �� **����(cascade)**�� ���� ������ �ݺ�.
 */
public class MatchGameLoop : MonoBehaviour
{
    [Header("Refs")]
    public BoardState board;
    public TopFiller filler;
    public GravityWithSlide gravity;
    public MatchFinder matcher;

    [Header("Init")]
    public int initialCount = 1;

    void Start()
    {
        StartCoroutine(GameStartRoutine());
    }

    IEnumerator GameStartRoutine()
    {
        // �������ڸ��� 30ĭ ���� �� ä�� ���·� ����
        yield return StartCoroutine(filler.SpawnSequence(30, 0.05f, 0.12f));

        //StartCoroutine(ResolveAllMatchesThenIdle());
        // �ʿ��ϸ� �ʹݺ��� ��ġ ���� ���� ������ ���� ���� �Ʒ� �� �� ����
        yield return ResolveAllMatchesThenIdle();

        yield break;
    }
    public IEnumerator ResolveAllMatchesThenIdle()
    {
        while (true)
        {
            var matched = matcher.CollectAllMatches();
            if (matched.Count == 0) 
                yield break;

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
