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
        // 초기 30칸: 아래->위, 좌->우 순서로 채움
        var fillOrder = board.AllCells()
                            .OrderBy(c => c.y)
                            .ThenBy(c => c.x)
                            .Take(initialCount)
                            .ToList();
        filler.FillCells(fillOrder);
        // 초기 해소
        yield return ResolveAllMatchesThenIdle();
    }

    public IEnumerator ResolveAllMatchesThenIdle()
    {
        while (true)
        {
            var matched = matcher.CollectAllMatches();
            if (matched.Count == 0) yield break;

            // 동시 제거
            foreach (var c in matched)
            {
                if (board.pieces.TryGetValue(c, out var go))
                {
                    Destroy(go);
                    board.pieces.Remove(c);
                }
            }

            // 제거 개수만큼 즉시 스폰 + 낙하 동시 처리
            yield return StartCoroutine(gravity.ApplyWithSpawn(matched.Count));
        }
    }
}
