using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
    public int initialTopCount = 5; // ★ 기본 5개


    // ─────────────────────────────────────────────────────────────
    // 칸 길이 기반 duration 계산 (입구 연출 속도 통일용)
    // ─────────────────────────────────────────────────────────────
    private float _cellStepLength = -1f;

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
        int want = (topCountOverride >= 0) ? topCountOverride : initialTopCount;
        want = Mathf.Clamp(want, 0, totalPieces);

        // 스폰 순서(0..totalPieces-1) 중 팽이를 넣을 인덱스 뽑기
        var topIdx = new HashSet<int>();
        while (topIdx.Count < want) topIdx.Add(Random.Range(0, totalPieces));

        for (int i = 0; i < totalPieces; i++)
        {
            bool spawnTop = topIdx.Contains(i);
            yield return SpawnFromTopEntry(spawnTop); // ★ 아래 함수 변경
            yield return CollapseAnimated();
        }
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

    // (핵심) (3,0) 위에서 등장 → (3,0) 열의 최상단 빈칸(입구)에 '등록' → 입구까지 짧게 이동
    IEnumerator SpawnFromTopEntry(bool spawnTop = false)
    {
        Vector3Int baseCell = new Vector3Int(3, 0, 0);
        Vector3Int entry = FindTopEntryAbove(baseCell);

        if (!board.IsValidCell(entry) || !board.IsEmpty(entry)) yield break;

        GameObject go;

        if (spawnTop && filler.topPrefab != null)
        {
            // ★ 팽이 생성
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
            // 일반 퍼즐 생성
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

            // 아래부터 처리(작은 y) → 충돌 최소화
            var unstable = board.pieces
                .Where(kv => HasDownwardEmpty(kv.Key))
                .OrderBy(kv => board.WorldCenter(kv.Key).y) // 아래부터
                .ToList();

            var moves = new List<(Transform tr, Vector3Int from, Vector3Int to)>();

            foreach (var kv in unstable)
            {
                var from = kv.Key;
                var go = kv.Value;

                if (!board.pieces.ContainsKey(from)) continue;

                // 🔽 한 칸만!
                var step = FindNextFallStep(from);
                if (step == from) continue;

                moved = true;

                // 보드 상태 즉시 반영(이번 라운드에서 겹침 방지)
                board.pieces.Remove(from);
                board.pieces[step] = go;

                moves.Add((go.transform, from, step));
            }

            // 애니메이션 실행(약간의 계단식 지연)
            if (moves.Count > 0)
            {
                var coroutines = new List<Coroutine>();
                for (int i = 0; i < moves.Count; i++)
                {
                    var (tr, _, to) = moves[i];
                    float delay = i * staggerDelay;
                    coroutines.Add(StartCoroutine(MoveWithDelay(tr, board.WorldCenter(to), delay)));
                }
                foreach (var c in coroutines) yield return c;
            }

        } while (moved && safety < 128);
    }

    // ====== 이동 유틸 ======

    private IEnumerator MoveWithDelay(Transform tr, Vector3 target, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        yield return MoveTo(tr, target, moveDurPerCell); // 셀 1칸 기준 시간
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
