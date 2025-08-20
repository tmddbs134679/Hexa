using UnityEngine;

/// Flat-Top + odd-r(행 오프셋). 좌표: (x=col, y=row), y는 위로 +1
public static class PuzzleDirs
{
    // 6방향 인덱스: 0:E, 1:NE, 2:NW, 3:W, 4:SW, 5:SE
    public static Vector3Int Step(Vector3Int c, int dir)
    {
        bool odd = (c.y & 1) == 1; // odd-r
        switch (dir)
        {
            case 0: return new Vector3Int(c.x + 1, c.y, 0);                         // E
            case 3: return new Vector3Int(c.x - 1, c.y, 0);                         // W
            case 1: return odd ? new Vector3Int(c.x + 1, c.y + 1, 0) : new Vector3Int(c.x, c.y + 1, 0); // NE
            case 2: return odd ? new Vector3Int(c.x, c.y + 1, 0) : new Vector3Int(c.x - 1, c.y + 1, 0); // NW
            case 4: return odd ? new Vector3Int(c.x, c.y - 1, 0) : new Vector3Int(c.x - 1, c.y - 1, 0); // SW
            case 5: return odd ? new Vector3Int(c.x + 1, c.y - 1, 0) : new Vector3Int(c.x, c.y - 1, 0); // SE
            default: return c;
        }
    }

    // 3개 축(양/음 방향쌍)
    // axis[0] = +방향, axis[1] = -방향 (MatchFinder에서 이 전제를 사용)
    public static readonly int[][] AXES = new int[][]
    {
        new []{ 0, 3 }, // E <-> W
        new []{ 1, 4 }, // NE <-> SW
        new []{ 2, 5 }, // NW <-> SE
    };
}
