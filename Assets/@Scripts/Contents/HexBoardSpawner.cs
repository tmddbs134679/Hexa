using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class HexBoardSpawner : MonoBehaviour
{
    [Header("Refs")]
    public Tilemap tilemap;            // 보드(헥사) 타일이 칠해진 Tilemap
    public GameObject puzzlePrefab;    // 퍼즐 프리팹(피벗 중앙)
    public Transform pieceParent;      // 정리용 부모(옵션)

    [Header("Timing")]
    public float spawnOffsetY = 4f;    // 꼭대기 셀의 월드Y + 이만큼 위에서 스폰
    public float fallDuration = 0.35f; // 낙하 시간
    public float interval = 0.06f;     // 개별 스폰 간격
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private List<Vector3Int> targets = new List<Vector3Int>();
    private Vector3 spawnPos;          // 항상 같은 스폰 위치(맨 꼭대기)

    void Start()
    {
        BuildTargetsAndSpawnPos();
        StartCoroutine(FillBottomLeftToRight());
    }

    // 1) 타깃 칸 순서 만들기 & 맨 꼭대기 스폰 위치 계산
    void BuildTargetsAndSpawnPos()
    {
        targets.Clear();
        BoundsInt b = tilemap.cellBounds;

        // 보드에 칠해진 모든 셀 수집
        foreach (var c in b.allPositionsWithin)
            if (tilemap.HasTile(c))
                targets.Add(c);

        if (targets.Count == 0)
        {
            Debug.LogWarning("No tiles on tilemap.");
            return;
        }

        // 타깃 순서: "아래 행 → 위 행" & "같은 행 안에서는 왼→오"
        // 헥사에서도 cell.y(행), cell.x(열) 기준이면 자연스러움
        targets = targets
            .OrderBy(c => c.y)   // 아래가 먼저
            .ThenBy(c => c.x)    // 같은 행이면 왼쪽이 먼저
            .ToList();

        // 스폰 위치: 보드 최상단(가장 큰 y)의 셀 중심 x를 기준으로, 그 위로 spawnOffsetY
        var topCell = targets.OrderByDescending(c => c.y).First();
        Vector3 topWorld = tilemap.GetCellCenterWorld(topCell);
        spawnPos = new Vector3(topWorld.x, topWorld.y + spawnOffsetY, topWorld.z);
    }

    // 2) 꼭대기 한 지점에서 하나씩 떨어뜨려 순서대로 채우기
    IEnumerator FillBottomLeftToRight()
    {
        for (int i = 0; i < targets.Count && i < 30; i++)
        {
            Vector3Int cell = targets[i];
            Vector3 targetWorld = tilemap.GetCellCenterWorld(cell);

            var go = Instantiate(
                puzzlePrefab,
                spawnPos,
                Quaternion.identity,
                pieceParent != null ? pieceParent : transform
            );

            yield return StartCoroutine(FallLerp(go.transform, spawnPos, targetWorld, fallDuration));
            yield return new WaitForSeconds(interval);
        }
    }

    // 보간 낙하(중력처럼 보이게)
    IEnumerator FallLerp(Transform t, Vector3 from, Vector3 to, float dur)
    {
        float e = 0f;
        while (e < dur)
        {
            float k = ease.Evaluate(e / dur);        // EaseOut 계열 추천
            t.position = Vector3.LerpUnclamped(from, to, k);
            e += Time.deltaTime;
            yield return null;
        }
        t.position = to;
    }
}
