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

    // 초기 채우기 등에 사용
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

        // 간단 이동 연출 (3,0 위쪽에서 목표 셀로)
        float t = 0f;
        while (t < fallDur)
        {
            float u = t / Mathf.Max(fallDur, 0.0001f);
            go.transform.position = Vector3.Lerp(from, target, u);
            t += Time.deltaTime;
            yield return null;
        }
        go.transform.position = target;
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