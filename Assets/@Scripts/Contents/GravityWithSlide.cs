using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
// ★ 착지 이펙트용
using DG.Tweening;

public class GravityWithSlide : MonoBehaviour
{
    public BoardState board;
    public TopFiller filler;

    [Header("Anim")]
    public float moveDurPerCell = 0.07f;   // 셀 1칸 이동 시간
    public float staggerDelay = 0.02f;     // 여러 조각 이동 시 계단식 지연

    // ★ 추가: 입구로 내려오는 구간도 칸당 속도와 동일하게 맞출지
    public bool unifyEntrySpeedWithPerCell = true;

    // ★ 선택: 위 옵션을 끄면 이 고정값을 사용
    public float entryMoveDuration = 0.12f;

    [Header("Hex Gravity Settings")]
    public float verticalThreshold = 0.3f; // "같은 열"로 취급할 x 허용치

    // 좌/우 동률일 때 번갈아 선택하기 위한 상태
    private int slideParity = 0;

    [Header("Initial Tops")]
    public int initialTopCount = 5; // 기본 5개

    // ─────────────────────────────────────────────────────────────
    // 칸 길이 기반 duration 계산 (입구 연출 속도 통일용)
    // ─────────────────────────────────────────────────────────────
    private float _cellStepLength = -1f;

    // ===========================================
    // ★ 착지 이펙트 옵션/상태
    // ===========================================
    [Header("Landing FX")]
    public bool useLandingFX = true;
    public bool landingFXOnInitialFill = true;

    // 타이밍
    [Range(0.08f, 0.7f)] public float landTotalDuration = 0.32f; // 전체 모션 시간
    [Range(0.15f, 0.8f)] public float landUpPortion = 0.45f;  // 전체 중 '상승' 비율(0~1)

    // 모양
    [Range(0f, 0.5f)] public float landBounceHeight = 0.12f;   // 위로 튀는 높이(월드)
    [Range(0f, 0.3f)] public float landCircleRadius = 0.06f;   // 동그라미 반지름(월드)
    [Range(1, 3)] public int landRevolutions = 1;       // 회전 횟수(1=한 바퀴)
    public bool landCircleClockwise = true;                        // 회전 방향
    public bool shrinkRadiusOnDescent = true;                      // 하강 단계에서 반지름 감쇠

    private bool _isInitialFillPhase = false;

    // ===========================================
    // ★ 착지 이펙트 본체
    // ===========================================
    void PlayLandingFX(Transform tr)
    {
        if (!useLandingFX || tr == null) return;
        if (_isInitialFillPhase && !landingFXOnInitialFill) return;

        tr.DOKill(false);

        Vector3 center = tr.position; // 최종 착지점
        float dur = Mathf.Max(0.01f, landTotalDuration);
        float upP = Mathf.Clamp01(landUpPortion);
        float R = Mathf.Max(0f, landCircleRadius);
        float H = Mathf.Max(0f, landBounceHeight);
        float dir = landCircleClockwise ? -1f : 1f;
        int revs = Mathf.Max(1, landRevolutions);

        // 0→1 진행률을 직접 받아, x/y/z를 동시에 세팅
        DOVirtual.Float(0f, 1f, dur, t =>
        {
            // 각도: 진행률에 비례해 revs바퀴 회전
            float ang = dir * (Mathf.PI * 2f * revs) * t;

            // 반지름: 상승 구간에선 고정, 하강 구간에서만 줄이기(옵션)
            float r;
            if (shrinkRadiusOnDescent && t > upP)
            {
                float td = (t - upP) / Mathf.Max(0.0001f, 1f - upP);     // 0~1
                r = Mathf.Lerp(R, 0f, EaseInOutQuad(td));                // 감쇠
            }
            else r = R;

            // 수직(y): 처음엔 위로 튀고(OutCubic), 이후 부드럽게 중심으로 복귀(InCubic)
            float y;
            if (t <= upP)
            {
                float tu = t / Mathf.Max(0.0001f, upP);
                y = center.y + H * EaseOutCubic(tu);
            }
            else
            {
                float td = (t - upP) / Mathf.Max(0.0001f, 1f - upP);
                y = center.y + H * (1f - EaseInCubic(td));
            }

            // 수평(x,z): 작은 원 궤적
            float x = center.x + r * Mathf.Cos(ang);
            float z = center.z + r * Mathf.Sin(ang);

            tr.position = new Vector3(x, y, z);
        })
        .SetEase(Ease.Linear)
        .OnComplete(() => tr.position = center); // 미세 오차 스냅
    }

    // --- 보조 이징 함수들 ---
    static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
    static float EaseInCubic(float x) => x * x * x;
    static float EaseInOutQuad(float x) => (x < 0.5f) ? 2f * x * x : 1f - Mathf.Pow(-2f * x + 2f, 2f) / 2f;

    // (3,0) 기준으로 "가장 아래" 이웃 한 칸의 월드 길이를 측정해서 캐시
    float GetCellStepLength()
    {
        if (_cellStepLength > 0f) return _cellStepLength;

        Vector3Int baseCell = new Vector3Int(3, 0, 0);
        if (!board.IsValidCell(baseCell))
        {
            _cellStepLength = 1f; // fallback
            return _cellStepLength;
        }

        Vector3 baseW = board.WorldCenter(baseCell);
        float bestDy = 0f;      // 가장 음수(더 아래)인 y차
        float bestDist = -1f;

        // 6방향 중 '아래'로 더 낮은 이웃을 찾음
        for (int i = 0; i < 6; i++)
        {
            var n = PuzzleDirs.Step(baseCell, i);
            if (!board.IsValidCell(n)) continue;

            Vector3 nw = board.WorldCenter(n);
            float dy = nw.y - baseW.y;
            float dist = Vector3.Distance(baseW, nw);

            if (dy < bestDy) // 더 아래쪽(더 작은 y)
            {
                bestDy = dy;
                bestDist = dist;
            }
        }

        if (bestDist > 0f)
        {
            _cellStepLength = bestDist;
            return _cellStepLength;
        }

        // 만약 '아래' 이웃을 못 찾으면(맵 회전 등) 임의의 인접 이웃 거리로 대체
        for (int i = 0; i < 6; i++)
        {
            var n = PuzzleDirs.Step(baseCell, i);
            if (!board.IsValidCell(n)) continue;

            float dist = Vector3.Distance(baseW, board.WorldCenter(n));
            if (dist > 0f)
            {
                _cellStepLength = dist;
                return _cellStepLength;
            }
        }

        _cellStepLength = 1f; // 최후의 보루
        return _cellStepLength;
    }

    // 두 지점 사이 거리를 '칸 수'로 환산해서 moveDurPerCell에 맞춘 duration을 반환
    float DurationByPerCell(Vector3 from, Vector3 to)
    {
        float d = Vector3.Distance(from, to);
        float one = Mathf.Max(0.0001f, GetCellStepLength());
        float cells = d / one; // 실수 칸수
        return cells * Mathf.Max(0.0001f, moveDurPerCell);
    }

    // ====== 초기 채우기 & 스폰 흐름 ======

    // 처음 채우기: (3,0) 입구로 넣고 → Collapse로 "한 칸씩" 흘러내리기
    public IEnumerator FillInitialBoard(int totalPieces, int topCountOverride = -1)
    {
        _isInitialFillPhase = true; // ★ 초기 스폰 구간 시작

        int want = (topCountOverride >= 0) ? topCountOverride : initialTopCount;
        want = Mathf.Clamp(want, 0, totalPieces);

        // 스폰 순서(0..totalPieces-1) 중 팽이를 넣을 인덱스 뽑기
        var topIdx = new HashSet<int>();
        while (topIdx.Count < want) topIdx.Add(Random.Range(0, totalPieces));

        for (int i = 0; i < totalPieces; i++)
        {
            bool spawnTop = topIdx.Contains(i);
            yield return SpawnFromTopEntry(spawnTop); // 입구까지 이동
            yield return CollapseAnimated();          // 실제 낙하
        }

        _isInitialFillPhase = false; // ★ 초기 스폰 구간 종료
    }

    // 매치 후 스폰도 동일한 규칙
    public IEnumerator ApplyWithSpawn(int spawnCount)
    {
        // 먼저 기존 블록들 정착
        yield return CollapseAnimated();

        for (int i = 0; i < spawnCount; i++)
        {
            // 🔁 매번 입구 비움 → 1개 스폰 → 다시 낙하
            yield return SpawnFromTopEntry();   // 입구(3,0) 최상단 빈칸에 등록 + 짧은 이동
            yield return CollapseAnimated();    // 한 칸씩 흘러내리며 자리 비워짐
        }
    }

    IEnumerator SpawnFromTopEntry(bool spawnTop = false)
    {
        Vector3Int baseCell = new Vector3Int(3, 0, 0);
        Vector3Int entry = FindTopEntryAbove(baseCell);
        if (!board.IsValidCell(entry) || !board.IsEmpty(entry)) yield break;

        GameObject go;
        if (spawnTop && filler.topPrefab != null)
        {
            go = Instantiate(filler.topPrefab);
            var top = go.GetComponent<SpinningTop>();
            if (top != null && filler.topSprite != null)
            {
                if (top.sr == null) top.sr = go.GetComponent<SpriteRenderer>();
                top.sr.sprite = filler.topSprite;
            }
        }
        else
        {
            int type = Random.Range(0, Mathf.Min(filler.colorCount, filler.typeSprites.Length));
            go = Instantiate(filler.piecePrefab);
            var pz = go.GetComponent<Puzzle>();
            pz.SetType(type, filler.typeSprites[type]);
        }

        // 화면 위에서 등장 → 입구셀 등록
        Vector3 from = board.WorldCenter(baseCell) + Vector3.up * filler.spawnHeightOffset;
        go.transform.position = from;

        board.pieces[entry] = go;

        Vector3 entryPos = board.WorldCenter(entry);
        float dur = unifyEntrySpeedWithPerCell ? DurationByPerCell(from, entryPos) : entryMoveDuration;
        yield return MoveTo(go.transform, entryPos, dur);

       // 스폰 직후 그 칸이 '이미 최종 자리'면, 여기서 바로 착지 FX 발동
        bool stableAtEntry = !HasDownwardEmpty(entry);
        if (stableAtEntry)
            PlayLandingFX(go.transform);
    }


    // (3,0)과 같은 '입구 열'의 최상단 빈칸 찾기
    private Vector3Int FindTopEntryAbove(Vector3Int entryBase)
    {
        float ex = board.WorldCenter(entryBase).x;

        var column = board.AllCells()
            .Where(c => Mathf.Abs(board.WorldCenter(c).x - ex) < verticalThreshold)
            .OrderByDescending(c => board.WorldCenter(c).y);

        foreach (var c in column)
            if (board.IsEmpty(c)) return c;

        // 못 찾으면 기본값 반환(호출부에서 비었는지 한 번 더 검사)
        return entryBase;
    }

    // ====== 중력 패스: "한 칸씩" 흘러내리기 ======

    // 아래로 더 낮은 빈칸이 있으면 "불안정" (= 계속 내려가야 함)
    private bool HasDownwardEmpty(Vector3Int cell)
    {
        var cw = board.WorldCenter(cell);
        for (int i = 0; i < 6; i++)
        {
            var n = PuzzleDirs.Step(cell, i);
            if (!board.IsValidCell(n) || !board.IsEmpty(n)) continue;

            var nw = board.WorldCenter(n);
            if (nw.y < cw.y - 0.01f) return true; // 더 낮은 방향만
        }
        return false;
    }

    // 현재 셀에서 "한 칸"만 아래 방향으로 이동할 다음 이웃 선택
    // - 수직 우선(같은 열이면 보너스)
    // - 그게 없으면 아래 대각(좌/우) 중 더 낮고 더 수직에 가까운 쪽
    // - 동률이면 좌/우 번갈이
    private Vector3Int FindNextFallStep(Vector3Int from)
    {
        var fw = board.WorldCenter(from);

        float bestScore = float.PositiveInfinity;
        var bests = new List<Vector3Int>();

        for (int i = 0; i < 6; i++)
        {
            var n = PuzzleDirs.Step(from, i);
            if (!board.IsValidCell(n) || !board.IsEmpty(n)) continue;

            var nw = board.WorldCenter(n);
            float dy = fw.y - nw.y;               // 아래쪽만 허용
            if (dy <= 0.01f) continue;

            float dx = Mathf.Abs(fw.x - nw.x);    // 수직에 가까울수록 우선
            float verticalBonus = (dx < verticalThreshold) ? -50f : 0f;

            // 더 낮을수록(= y가 작을수록), 더 수직일수록 점수 ↓
            float score = nw.y * 100f + dx * 10f + verticalBonus;

            const float EPS = 0.001f;
            if (score < bestScore - EPS)
            {
                bestScore = score;
                bests.Clear();
                bests.Add(n);
            }
            else if (Mathf.Abs(score - bestScore) <= EPS)
            {
                bests.Add(n);
            }
        }

        if (bests.Count == 0) return from; // 더 내려갈 곳 없음 → 제자리

        if (bests.Count > 1)
        {
            // 동률이면 좌/우 번갈이: x가 작은 쪽(왼) ↔ x가 큰 쪽(오)
            bool pickLeft = (slideParity & 1) == 0;
            slideParity ^= 1;

            bests.Sort((a, b) =>
            {
                float ax = board.WorldCenter(a).x;
                float bx = board.WorldCenter(b).x;
                return ax.CompareTo(bx);
            });
            return pickLeft ? bests.First() : bests.Last();
        }

        return bests[0];
    }

    // 보드 안정화: 아래로 갈 곳 있는 조각들을 "한 칸씩" 이동 → 몇 라운드 반복
    public IEnumerator CollapseAnimated()
    {
        bool moved;
        int safety = 0;

        do
        {
            moved = false;
            safety++;

            var unstable = board.pieces
                .Where(kv => HasDownwardEmpty(kv.Key))
                .OrderBy(kv => board.WorldCenter(kv.Key).y) // 아래부터
                .ToList();

            var tasks = new List<Coroutine>();

            int i = 0;
            foreach (var kv in unstable)
            {
                var from = kv.Key;
                var go = kv.Value;

                if (!board.pieces.ContainsKey(from)) continue;

                var to = FindNextFallStep(from);
                if (to == from) continue;

                moved = true;

                // 보드 즉시 갱신(동시에 겹치지 않게)
                board.pieces.Remove(from);
                board.pieces[to] = go;

                // ★ 이 이동이 "마지막"인지 즉시 판정
                bool isFinalAfterThisStep = !HasDownwardEmpty(to);

                float delay = i * staggerDelay; // 계단식 딜레이
                tasks.Add(StartCoroutine(
                    MoveWithDelay(go.transform, board.WorldCenter(to), delay, isFinalAfterThisStep)
                ));
                i++;
            }

            // 이번 라운드 이동 실행
            foreach (var c in tasks) yield return c;

        } while (moved && safety < 128);
    }


    // ====== 이동 유틸 ======

    private IEnumerator MoveWithDelay(Transform tr, Vector3 target, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        yield return MoveTo(tr, target, moveDurPerCell); // 셀 1칸 기준 시간
    }
    private IEnumerator MoveWithDelay(Transform tr, Vector3 target, float delay, bool landingFx)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        yield return MoveTo(tr, target, moveDurPerCell);
        if (landingFx) PlayLandingFX(tr); // ★ 도착 즉시 개별 실행
    }

    private IEnumerator MoveTo(Transform tr, Vector3 to, float dur)
    {
        Vector3 from = tr.position;
        float t = 0f;

        while (t < dur)
        {
            float u = Mathf.Clamp01(t / Mathf.Max(0.0001f, dur));
            // 가속 느낌(중력감)
            float eased = u * u;
            tr.position = Vector3.LerpUnclamped(from, to, eased);
            t += Time.deltaTime;
            yield return null;
        }
        tr.position = to;
    }
}
