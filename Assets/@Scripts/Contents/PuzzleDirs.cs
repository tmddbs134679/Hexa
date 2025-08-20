using UnityEngine;

/// Flat-Top + odd-q(열 오프셋). 좌표: (x=col, y=row), y는 위로 +1
public static class PuzzleDirs
{
    static bool IsOddCol(int x) => (x & 1) == 1;   //좌표값 기준으로 홀짝 판정

    // 수평(매칭용만 사용, 중력에는 사용X)
    public static Vector3Int E(Vector3Int c) => new(c.x + 1, c.y, 0);
    public static Vector3Int W(Vector3Int c) => new(c.x - 1, c.y, 0);

    // 수직
    public static Vector3Int N(Vector3Int c) => new(c.x, c.y + 1, 0);
    public static Vector3Int S(Vector3Int c) => new(c.x, c.y - 1, 0);   // ↓

    // 대각 (열 홀짝에 따라 다름) — y는 위가 +1이므로, 아래는 대체로 y-1
    public static Vector3Int SE(Vector3Int c) => IsOddCol(c.x) ? new(c.x + 1, c.y, 0)
                                                               : new(c.x + 1, c.y - 1, 0);
    public static Vector3Int SW(Vector3Int c) => IsOddCol(c.x) ? new(c.x - 1, c.y, 0)
                                                               : new(c.x - 1, c.y - 1, 0);
    public static Vector3Int NE(Vector3Int c) => IsOddCol(c.x) ? new(c.x + 1, c.y + 1, 0)
                                                               : new(c.x + 1, c.y, 0);
    public static Vector3Int NW(Vector3Int c) => IsOddCol(c.x) ? new(c.x - 1, c.y + 1, 0)
                                                               : new(c.x - 1, c.y, 0);

    //  중력 우선순위: 아래(S) → 아래오른쪽(SE) → 아래왼쪽(SW)
    public static Vector3Int Down(Vector3Int c) => S(c);
    public static Vector3Int DownRight(Vector3Int c) => SE(c);
    public static Vector3Int DownLeft(Vector3Int c) => SW(c);

    // MatchFinder 호환: 0..5 = E, SE, SW, W, NW, NE
    public static Vector3Int Step(Vector3Int c, int dir) => dir switch
    {
        0 => E(c),
        1 => SE(c),
        2 => SW(c),
        3 => W(c),
        4 => NW(c),
        _ => NE(c), // 5
    };

    // 직선 축쌍(3축): E-W, SE-NW, SW-NE
    public static readonly int[][] AXES =
    {
        new[]{0,3}, // E-W
        new[]{1,4}, // SE-NW
        new[]{2,5}, // SW-NE
    };
}
