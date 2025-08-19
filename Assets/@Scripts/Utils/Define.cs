using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Define 
{
    static readonly Vector3Int[] DIR_EVEN = { new(+1,0,0), new(0,+1,0), new(-1,+1,0),
                                               new(-1,0,0), new(0,-1,0), new(+1,-1,0) };
    static readonly Vector3Int[] DIR_ODD = { new(+1,0,0), new(+1,+1,0), new(0,+1,0),
                                               new(-1,0,0), new(-1,-1,0), new(0,-1,0) };
    static Vector3Int Dir(Vector3Int c, int i)
    {
        var d = ((c.x & 1) == 0) ? DIR_EVEN[i] : DIR_ODD[i];
        return c + d;
    }

    // 축 3개(반대방향 쌍): 0↔3, 1↔4, 2↔5
    static readonly int[][] AXES = { new[] { 0, 3 }, new[] { 1, 4 }, new[] { 2, 5 } };

    public enum EUIEvent
    {
        None,
        Click,
        Preseed,
        PointerDown,
        PointerUp,
        BeginDrag,
        Drag,
        EndDrag,

    }
    public enum ESound
    {
        None,
        Bgm,
        SubBgm,
        Effect,
        Max,

    }

    public enum EObjectType
    {
        Blue,
        Green,
        Orange,
        Purple,
        Red,
        Yellow,
        None,

    }

    public enum EScene
    {
        None,
        TitleScene,
        GameScene,
    }
}
