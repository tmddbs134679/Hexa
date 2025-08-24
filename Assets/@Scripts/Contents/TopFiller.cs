using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/*
 * TopFiller — "스폰 전담"
 * - 비어있는 셀을 골라, 그 셀의 위쪽(World Y+)에서 퍼즐 프리팹을 생성해 내려 꽂음
 * - 생성/등록만 담당 (board.pieces 갱신 포함)
 * - 낙하 슬라이드는 GravityWithSlide에서 처리
 */
public class TopFiller : MonoBehaviour
{
    public BoardState board;
    [Header("Spawn")]
    public GameObject piecePrefab;
    public Sprite[] typeSprites;        // 색 스프라이트(0~N-1)
    public int colorCount = 6;          // 사용 색 개수(장애물/특수 제외)

    [Header("Spawn Position")]
    public float spawnHeightOffset = 2.0f; // (3,0) 위쪽으로 얼마나 올릴지

    [Header("Flowing Animation")]
    public bool useFlowingAnimation = true;     // 흘러내리는 애니메이션 사용 여부
    public float flowStepDuration = 0.08f;      // 각 단계별 이동 시간
    public int maxFlowSteps = 8;               // 최대 흘러내리기 단계 수

    // 초기 채우기 등에 사용
    public IEnumerator SpawnSequence(int count, float interval, float fallDur)
    {
       
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

    // 특정 셀 채우기(위치 애니메이션 포함)
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

        // 등록
        board.pieces[cell] = go;

        if (useFlowingAnimation)
        {
            // 흘러내리는 연출
            yield return StartCoroutine(FlowToTarget(go.transform, from, target));
        }
        else
        {
            // 기존 직선 이동
            yield return StartCoroutine(DirectMove(go.transform, from, target, fallDur));
        }
    }

    // 흘러내리는 애니메이션 - 중간 경로점들을 통해 자연스럽게 이동
    IEnumerator FlowToTarget(Transform transform, Vector3 start, Vector3 target)
    {
        List<Vector3> waypoints = CalculateHexFlowPath(start, target);

        for (int i = 1; i < waypoints.Count; i++)
        {
            Vector3 from = waypoints[i - 1];
            Vector3 to = waypoints[i];

            yield return StartCoroutine(DirectMove(transform, from, to, flowStepDuration));
        }
    }



    // 더 자연스러운 흘러내리기 경로 (헥사곤 그리드 고려)
    List<Vector3> CalculateHexFlowPath(Vector3 start, Vector3 target)
    {
        List<Vector3> waypoints = new List<Vector3>();
        waypoints.Add(start);

        // 스폰 지점에서 목표까지의 가상 경로를 헥사곤 그리드 방향으로 계산
        Vector3Int spawnCell = new Vector3Int(3, 0, 0);
        Vector3Int targetCell = board.tilemap.WorldToCell(target);

        Vector3 current = start;

        // 일단 아래로 떨어뜨리기
        Vector3 dropPoint = new Vector3(start.x, target.y + 1f, start.z);
        waypoints.Add(dropPoint);

        // 목표 쪽으로 흘러가기
        Vector3 midPoint = Vector3.Lerp(dropPoint, target, 0.7f);
        waypoints.Add(midPoint);

        // 최종 목표
        waypoints.Add(target);

        return waypoints;
    }

    // 직선 이동 (기존 방식)
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

    // 여러 셀 한꺼번에
    public IEnumerator SpawnIntoMany(IEnumerable<Vector3Int> cells, float fallDur, float between = 0.02f)
    {
        foreach (var c in cells)
        {
            yield return SpawnInto(c, fallDur);
            if (between > 0) yield return new WaitForSeconds(between);
        }
    }
}