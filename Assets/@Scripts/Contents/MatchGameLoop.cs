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
        // 애니 동기화가 필요하면 Animator 이벤트로 대체 가능
        yield return new WaitForSeconds(waitForAnim);

        var dead = board.pieces
            .Where(kv => kv.Value.GetComponent<SpinningTop>()?.armedToBreak == true)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var cell in dead)
        {
            var go = board.pieces[cell];
            board.pieces.Remove(cell);
            Destroy(go);
        }
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
}
