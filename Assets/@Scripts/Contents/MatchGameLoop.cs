using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

/*
 매치-3(헥사) 게임 루프를 코루틴으로 돌림: 초기 배치 → 매치 제거 → 낙하/보충 → 다시 매치… 를 **연쇄(cascade)**가 끝날 때까지 반복.
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
        // 시작하자마자 30칸 전부 꽉 채운 상태로 세팅
        yield return StartCoroutine(filler.SpawnSequence(30, 0.05f, 0.12f));

        //StartCoroutine(ResolveAllMatchesThenIdle());
        // 필요하면 초반부터 매치 제거 루프 돌리고 싶을 때만 아래 한 줄 유지
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
