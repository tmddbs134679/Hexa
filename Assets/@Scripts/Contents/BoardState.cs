using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;



/*

핵심 역할 요약

보드 셀 관리

CacheValidCells() : 타일이 실제 있는 셀만 수집해 유효 셀 집합(validCells)으로 저장

IsValidCell(c) : 주어진 좌표가 보드 내 유효 셀인지 판정

퍼즐 오브젝트 관리

pieces : 셀 좌표 → 퍼즐 오브젝트 매핑 저장 (현재 어떤 셀에 무슨 오브젝트가 있는지 추적)

EmptyCells() : 아직 오브젝트가 없는 빈 셀 반환

IsEmpty(c) : 특정 셀이 비어있는지 확인

좌표 변환 지원

WorldCenter(c) : 셀 좌표를 월드 좌표의 셀 중심점으로 변환 → 오브젝트 스폰이나 이동 위치에 사용

*/
public class BoardState : MonoBehaviour
{
    [Header("Tilemap")]
    public Tilemap tilemap;                                // 헥사 보드 타일맵

    [Header("Runtime State")]
    public Dictionary<Vector3Int, GameObject> pieces = new();  // 셀 -> 퍼즐 오브젝트
    private HashSet<Vector3Int> validCells = new();             // 보드 내 유효 셀

    void Awake()
    {
        CacheValidCells();
    }


    public void CacheValidCells()
    {
        validCells.Clear();
        var b = tilemap.cellBounds;
        foreach (var c in b.allPositionsWithin)
            if (tilemap.HasTile(c))
                validCells.Add(c);
    }

    public IEnumerable<Vector3Int> AllCells() => validCells;

    public IEnumerable<Vector3Int> EmptyCells()
    {
        foreach (var c in validCells)
            if (!pieces.ContainsKey(c))
                yield return c;
    }

    public bool IsValidCell(Vector3Int c) => validCells.Contains(c);

    public bool IsEmpty(Vector3Int c) => IsValidCell(c) && !pieces.ContainsKey(c);

    public Vector3 WorldCenter(Vector3Int c) => tilemap.GetCellCenterWorld(c);  //셀의 정중앙 중심점 리턴
}
