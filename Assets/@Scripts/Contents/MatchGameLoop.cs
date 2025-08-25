using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using DG.Tweening;

/*
 매치-3(헥사) 게임 루프를 코루틴으로 돌림: 초기 배치 → 매치 제거 → 낙하/보충 → 다시 매치… 를 **연쇄(cascade)**가 끝날 때까지 반복.
 */
public partial class  MatchGameLoop : MonoBehaviour
{
    [Header("Refs")]
    public BoardState board;
    public TopFiller filler;
    public GravityWithSlide gravity;
    public MatchFinder matcher;
    
    [Header("Init")]
    public int initialCount = 1;
    
    [Header("Animation Settings")]
    public float destroyAnimDuration = 0.3f;
    public float destroyScaleMultiplier = 1.5f;
    public bool useSequentialDestroy = false;
    public float sequentialDelay = 0.05f;
    
    void Start()
    {
        StartCoroutine(GameStartRoutine());
    }
    
    IEnumerator GameStartRoutine()
    {
        // 시작하자마자 30칸 전부 꽉 채운 상태로 세팅
        yield return StartCoroutine(gravity.FillInitialBoard(30, 5));

        //  추가: 이번 레벨 목표/이동수 초기화(예: 이동 15, 팽이 5개)
        Managers.Game.BeginLevel(startMoves: 15, topGoal: 5);

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
                
            // 매치된 조각들의 파괴 애니메이션
            yield return StartCoroutine(DestroyMatchedPiecesWithAnimation(matched));

            yield return StartCoroutine(Tops_PostDestroyAndBeforeGravity(matched));

            // 제거 개수만큼 즉시 스폰 + 낙하 동시 처리
            int toSpawn = board.EmptyCells().Count();
            yield return StartCoroutine(gravity.ApplyWithSpawn(toSpawn));
        }
    }
    
    IEnumerator DestroyMatchedPiecesWithAnimation(HashSet<Vector3Int> matchedCells)
    {
        List<Transform> matchedTransforms = new List<Transform>();
        
        // 매치된 조각들의 Transform 수집
        foreach (var cell in matchedCells)
        {
            if (board.pieces.TryGetValue(cell, out var go) && go != null)
            {
                matchedTransforms.Add(go.transform);
            }
        }
        
        if (matchedTransforms.Count == 0)
        {
            // Transform이 없으면 그냥 보드에서만 제거
            foreach (var cell in matchedCells)
            {
                board.pieces.Remove(cell);
            }
            yield break;
        }
        
        if (useSequentialDestroy)
        {
            // 순차적 파괴 애니메이션
            yield return StartCoroutine(DestroySequentially(matchedTransforms, matchedCells));
        }
        else
        {
            // 동시 파괴 애니메이션
            yield return StartCoroutine(DestroySimultaneously(matchedTransforms, matchedCells));
        }
    }
    
    IEnumerator DestroySimultaneously(List<Transform> transforms, HashSet<Vector3Int> cells)
    {
        // 동시에 모든 조각 애니메이션 시작
        foreach (var transform in transforms)
        {
            if (transform != null)
            {
                AnimatePieceDestroy(transform, 0f);
            }
        }
        
        // 애니메이션 완료까지 대기
        yield return new WaitForSeconds(destroyAnimDuration);
        
        // 실제 오브젝트 제거 및 보드에서 삭제
        CleanupPieces(transforms, cells);
    }
    
    IEnumerator DestroySequentially(List<Transform> transforms, HashSet<Vector3Int> cells)
    {
        // 순차적으로 애니메이션 시작
        for (int i = 0; i < transforms.Count; i++)
        {
            var transform = transforms[i];
            if (transform != null)
            {
                float delay = i * sequentialDelay;
                AnimatePieceDestroy(transform, delay);
            }
        }
        
        // 모든 애니메이션 완료 대기 (마지막 조각의 시작 시간 + 애니메이션 시간)
        float totalTime = (transforms.Count - 1) * sequentialDelay + destroyAnimDuration;
        yield return new WaitForSeconds(totalTime);
        
        // 정리
        CleanupPieces(transforms, cells);
    }
    
    void AnimatePieceDestroy(Transform transform, float delay)
    {
        if (transform == null) return;
        
        // 크기 확대 + 회전
        transform.DOScale(Vector3.one * destroyScaleMultiplier, destroyAnimDuration)
                 .SetDelay(delay)
                 .SetEase(Ease.OutBack);
                 
        transform.DORotate(new Vector3(0, 0, 360), destroyAnimDuration, RotateMode.FastBeyond360)
                 .SetDelay(delay)
                 .SetEase(Ease.InOutQuad);
        
        // 페이드 아웃
        var spriteRenderer = transform.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.DOFade(0f, destroyAnimDuration * 0.7f)
                          .SetDelay(delay + destroyAnimDuration * 0.3f);
        }
        
        // CanvasGroup이 있다면 (UI 조각용)
        var canvasGroup = transform.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, destroyAnimDuration * 0.7f)
                       .SetDelay(delay + destroyAnimDuration * 0.3f);
        }
    }
    
    void CleanupPieces(List<Transform> transforms, HashSet<Vector3Int> cells)
    {
        // 1) 이번에 사라지는 칸들을 "연결된 덩어리(군집/라인)"별로 나눔
        var groups = SplitIntoGroups(cells);

        // 2) 그룹당 60점 + 팝업 1개(그룹의 중심 위치)
        foreach (var g in groups)
        {
            Vector3 center = GroupWorldCenter(g);
            Managers.Game.ShowScorePopup(60, center); // ★ 팝업 1개
        }
        Managers.Game.AddScore(groups.Count * 60);     // ★ 점수: 그룹 수 × 60

        // DoTween 정리 및 오브젝트 파괴
        foreach (var transform in transforms)
        {
            if (transform != null)
            {
                transform.DOKill(); // 메모리 누수 방지
                Destroy(transform.gameObject);
            }
        }
        
        // 보드에서 제거
        foreach (var cell in cells)
        {
            board.pieces.Remove(cell);
        }
    }


}
public partial class MatchGameLoop : MonoBehaviour
{
    // === 팽이 보조: 라운드 시작/스택 부여/파괴/클리어 체크 ===

    void Tops_RoundReset()
    {
        foreach (var go in board.pieces.Values)
        {
            var top = go.GetComponent<SpinningTop>();
            if (top) top.RoundReset();
        }
    }

    void Tops_ApplyStacks(HashSet<Vector3Int> clearedThisRound)
    {
        var touched = new HashSet<GameObject>(); // 라운드당 팽이 1회만
        foreach (var cell in clearedThisRound)
        {
            for (int d = 0; d < 6; d++)
            {
                var n = PuzzleDirs.Step(cell, d);
                if (!board.IsValidCell(n)) continue;
                if (!board.pieces.TryGetValue(n, out var go)) continue;

                var top = go.GetComponent<SpinningTop>();
                if (top != null && touched.Add(go))
                    top.OnAdjacentDestroyedThisRound();
            }
        }
    }

    IEnumerator Tops_RemoveArmed(float waitForAnim = 0.25f)
    {
        yield return new WaitForSeconds(waitForAnim);

        var dead = board.pieces
            .Where(kv => kv.Value.GetComponent<SpinningTop>()?.armedToBreak == true)
            .Select(kv => kv.Key)
            .ToList();

        if (dead.Count > 0)
        {
            // ★ 1) 팽이 위치마다 +500 팝업
            foreach (var cell in dead)
            {
                Vector3 pos = board.WorldCenter(cell);
                Managers.Game.ShowScorePopup(Define.POINT_SCORE_TOP, pos);
            }

            // ★ 2) 점수 총합 추가
            Managers.Game.AddScore(Define.POINT_SCORE_TOP * dead.Count);
        }

        // 실제 제거
        foreach (var cell in dead)
        {
            var go = board.pieces[cell];
            board.pieces.Remove(cell);
            Destroy(go);
        }

        // (기존 진행도/클리어 업데이트가 여기라면 유지)
        if (dead.Count > 0)
            Managers.Game.AddTopDestroyed(dead.Count);
    }

    bool Tops_AnyLeft()
        => board.pieces.Values.Any(go => go.GetComponent<SpinningTop>() != null);

    // === 외부에서 호출: "매치 조각 파괴 직후, 중력/보충 직전" ===
    public IEnumerator Tops_PostDestroyAndBeforeGravity(HashSet<Vector3Int> cleared)
    {
        Debug.Log($"[TOPS] ENTER cleared={cleared?.Count ?? -1}");   // ★ 진입 로그

        Tops_RoundReset();
        Tops_ApplyStacks(cleared);
        yield return StartCoroutine(Tops_RemoveArmed());

        // 모든 팽이 제거되면 스테이지 클리어
        //if (!Tops_AnyLeft())
        //    //OnStageCleared(); // 너의 기존 클리어 처리 호출
    }

    List<HashSet<Vector3Int>> SplitIntoGroups(HashSet<Vector3Int> cells)
    {
        var groups = new List<HashSet<Vector3Int>>();
        var visited = new HashSet<Vector3Int>();

        foreach (var start in cells)
        {
            if (visited.Contains(start)) continue;

            var g = new HashSet<Vector3Int>();
            var q = new Queue<Vector3Int>();
            visited.Add(start);
            q.Enqueue(start);

            while (q.Count > 0)
            {
                var c = q.Dequeue();
                g.Add(c);
                for (int d = 0; d < 6; d++)
                {
                    var n = PuzzleDirs.Step(c, d);
                    if (cells.Contains(n) && !visited.Contains(n))
                    {
                        visited.Add(n);
                        q.Enqueue(n);
                    }
                }
            }
            groups.Add(g);
        }
        return groups;
    }

    // 그룹의 월드 중심(평균) 좌표
    Vector3 GroupWorldCenter(HashSet<Vector3Int> group)
    {
        if (group == null || group.Count == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero;
        int cnt = 0;
        foreach (var c in group)
        {
            sum += board.WorldCenter(c);
            cnt++;
        }
        return sum / Mathf.Max(1, cnt);

    }
}
