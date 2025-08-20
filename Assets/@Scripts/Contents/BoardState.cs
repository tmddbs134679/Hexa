using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;



/*

�ٽ� ���� ���

���� �� ����

CacheValidCells() : Ÿ���� ���� �ִ� ���� ������ ��ȿ �� ����(validCells)���� ����

IsValidCell(c) : �־��� ��ǥ�� ���� �� ��ȿ ������ ����

���� ������Ʈ ����

pieces : �� ��ǥ �� ���� ������Ʈ ���� ���� (���� � ���� ���� ������Ʈ�� �ִ��� ����)

EmptyCells() : ���� ������Ʈ�� ���� �� �� ��ȯ

IsEmpty(c) : Ư�� ���� ����ִ��� Ȯ��

��ǥ ��ȯ ����

WorldCenter(c) : �� ��ǥ�� ���� ��ǥ�� �� �߽������� ��ȯ �� ������Ʈ �����̳� �̵� ��ġ�� ���

*/
public class BoardState : MonoBehaviour
{
    [Header("Tilemap")]
    public Tilemap tilemap;                                // ��� ���� Ÿ�ϸ�

    [Header("Runtime State")]
    public Dictionary<Vector3Int, GameObject> pieces = new();  // �� -> ���� ������Ʈ
    private HashSet<Vector3Int> validCells = new();             // ���� �� ��ȿ ��

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

    public Vector3 WorldCenter(Vector3Int c) => tilemap.GetCellCenterWorld(c);  //���� ���߾� �߽��� ����
}
