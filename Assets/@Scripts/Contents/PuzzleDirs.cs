using UnityEngine;

/// Flat-Top + odd-q(열 오프셋) 레이아웃 전용
/// Unity Tilemap 좌표(x=col, y=row, y가 위로 증가)
public static class PuzzleDirs
{
    // ── axial(q,r)에서의 이웃 (시계): E, SE, SW, W, NW, NE ──
    static readonly Vector2Int[] AXIAL_DIRS =
    {
        new(+1,  0), // 0 E
        new( 0, +1), // 1 SE
        new(-1, +1), // 2 SW
        new(-1,  0), // 3 W
        new( 0, -1), // 4 NW
        new(+1, -1), // 5 NE
    };

    // ── odd-q <-> axial 변환 (Flat-Top) ──
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

    // ── 중력 우선순위: 아래 → 오른쪽 → 왼쪽 ──
    // Flat-Top에서 '아래'는 대각선 하나입니다. (여기서는 SE를 아래로 정의)
    public static Vector3Int Down(Vector3Int c) => Step(c, 1); // SE
    public static Vector3Int DownRight(Vector3Int c) => Step(c, 0); // E
    public static Vector3Int DownLeft(Vector3Int c) => Step(c, 2); // SW

    // 직선 축쌍(3축)
    public static readonly int[][] AXES =
    {
        new[]{0,3}, // E-W
        new[]{1,4}, // SE-NW
        new[]{2,5}, // SW-NE
    };
}
