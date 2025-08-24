using System; // Obsolete attribute
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

/*
 * TopFiller — "스폰 전담"
 * - 프리팹/스킨 데이터 보관
 * - (과거) 비어있는 셀을 골라 해당 셀로 바로 꽂는 유틸들 포함 → 지금은 GravityWithSlide로 대체
 */
public class TopFiller : MonoBehaviour
{
    public BoardState board;

    [Header("Spawn")]
    public GameObject piecePrefab;
    public Sprite[] typeSprites;       // 색 스프라이트(0~N-1)
    public int colorCount = 6;         // 사용 색 개수(장애물/특수 제외)

    [Header("Spawn Position")]
    public float spawnHeightOffset = 2.0f; // (3,0) 위쪽으로 얼마나 올릴지

    [Header("Flowing Animation (deprecated path visuals)")]
    public bool useFlowingAnimation = true; // 흘러내리는 애니메이션 사용 여부
    public float flowStepDuration = 0.08f;  // 각 단계별 이동 시간
    public int maxFlowSteps = 8;            // 최대 흘러내리기 단계 수

    // ─────────────────────────────────────────────────────────────
    // 아래 메서드들은 이제 사용 금지: GravityWithSlide.FillInitialBoard / ApplyWithSpawn 사용
    // ─────────────────────────────────────────────────────────────

    [Obsolete("Use GravityWithSlide.FillInitialBoard / ApplyWithSpawn instead.")]
    public IEnumerator SpawnSequence(int count, float interval, float fallDur)
    {
        // 위에서부터 보이게: y가 큰(위쪽) 순으로 비어있는 셀을 선택
        var empties = board.EmptyCells()
                           .OrderBy(c => board.WorldCenter(c).y) // <- 아래부터
                           .Take(count)
                           .ToList();

        foreach (var cell in empties)
        {
            yield return SpawnInto(cell, fallDur);
            if (interval > 0) yield return new WaitForSeconds(interval);
        }
    }

    [Obsolete("Use GravityWithSlide.FillInitialBoard / ApplyWithSpawn instead.")]
    public IEnumerator SpawnInto(Vector3Int cell, float fallDur)
    {
        if (!board.IsEmpty(cell)) yield break;
        int type = Random.Range(0, Mathf.Min(colorCount, typeSprites.Length));
        var go = Instantiate(piecePrefab);
        var pz = go.GetComponent<Puzzle>();
        pz.SetType(type, typeSprites[type]);

        Vector3 target = board.WorldCenter(cell);

        // 모든 조각을 (3,0) 셀 위쪽에서 스폰
        Vector3Int spawnCell = new Vector3Int(3, 0, 0);
        Vector3 spawnBase = board.WorldCenter(spawnCell);
        Vector3 from = spawnBase + Vector3.up * spawnHeightOffset;

        go.transform.position = from;

        // 등록 (직접 삽입 방식 — 현재는 사용 비권장)
        board.pieces[cell] = go;

        if (useFlowingAnimation)
        {
            // 흘러내리는 연출
            yield return StartCoroutine(DirectMove(go.transform, from, new Vector3(from.x, target.y + 1f, from.z), flowStepDuration));
            yield return StartCoroutine(DirectMove(go.transform, new Vector3(from.x, target.y + 1f, from.z), Vector3.Lerp(new Vector3(from.x, target.y + 1f, from.z), target, 0.7f), flowStepDuration));
            yield return StartCoroutine(DirectMove(go.transform, Vector3.Lerp(new Vector3(from.x, target.y + 1f, from.z), target, 0.7f), target, flowStepDuration));
        }
        else
        {
            // 기존 직선 이동
            yield return StartCoroutine(DirectMove(go.transform, from, target, fallDur));
        }
    }

    // 직선 이동 유틸
    IEnumerator DirectMove(Transform transform, Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            float u = t / Mathf.Max(duration, 0.0001f);
            transform.position = Vector3.LerpUnclamped(from, to, u);
            t += Time.deltaTime;
            yield return null;
        }
        transform.position = to;
    }

    // 여러 셀 한꺼번에 (현재는 사용 비권장)
    [Obsolete("Use GravityWithSlide.FillInitialBoard / ApplyWithSpawn instead.")]
    public IEnumerator SpawnIntoMany(IEnumerable<Vector3Int> cells, float fallDur, float between = 0.02f)
    {
        foreach (var c in cells)
        {
            yield return SpawnInto(c, fallDur);
            if (between > 0) yield return new WaitForSeconds(between);
        }
    }
}
