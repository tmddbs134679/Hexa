using UnityEngine;

/// Flat-Top + odd-q(�� ������) ���̾ƿ� ����
/// Unity Tilemap ��ǥ(x=col, y=row, y�� ���� ����)
public static class PuzzleDirs
{
    // ���� axial(q,r)������ �̿� (�ð�): E, SE, SW, W, NW, NE ����
    static readonly Vector2Int[] AXIAL_DIRS =
    {
        new(+1,  0), // 0 E
        new( 0, +1), // 1 SE
        new(-1, +1), // 2 SW
        new(-1,  0), // 3 W
        new( 0, -1), // 4 NW
        new(+1, -1), // 5 NE
    };

    // ���� odd-q <-> axial ��ȯ (Flat-Top) ����
    // ref: redblobgames hex grids
    static Vector2Int ToAxial(Vector3Int cell) // (x,y)->(q,r)
    {
        int q = cell.x;
        int r = cell.y - ((cell.x - (cell.x & 1)) / 2);
        return new Vector2Int(q, r);
    }
    static Vector3Int FromAxial(Vector2Int ar) // (q,r)->(x,y)
    {
        int x = ar.x;
        int y = ar.y + ((ar.x - (ar.x & 1)) / 2);
        return new Vector3Int(x, y, 0);
    }

    public static Vector3Int Step(Vector3Int c, int dir)
    {
        var ar = ToAxial(c);
        var d = AXIAL_DIRS[dir];
        ar.x += d.x; ar.y += d.y;
        return FromAxial(ar);
    }

    // ���� �߷� �켱����: �Ʒ� �� ������ �� ���� ����
    // Flat-Top���� '�Ʒ�'�� �밢�� �ϳ��Դϴ�. (���⼭�� SE�� �Ʒ��� ����)
    public static Vector3Int Down(Vector3Int c) => Step(c, 1); // SE
    public static Vector3Int DownRight(Vector3Int c) => Step(c, 0); // E
    public static Vector3Int DownLeft(Vector3Int c) => Step(c, 2); // SW

    // ���� ���(3��)
    public static readonly int[][] AXES =
    {
        new[]{0,3}, // E-W
        new[]{1,4}, // SE-NW
        new[]{2,5}, // SW-NE
    };
}
