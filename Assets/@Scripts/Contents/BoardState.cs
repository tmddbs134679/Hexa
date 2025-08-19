using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

public class BoardState : MonoBehaviour
{
    [Header("Tilemap")]
    public Tilemap tilemap;                                // «ÌªÁ ∫∏µÂ ≈∏¿œ∏ 

    [Header("Runtime State")]
    public Dictionary<Vector3Int, GameObject> pieces = new();  // ºø -> ∆€¡Ò ø¿∫Í¡ß∆Æ
    private HashSet<Vector3Int> validCells = new();             // ∫∏µÂ ≥ª ¿Ø»ø ºø

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

    public Vector3 WorldCenter(Vector3Int c) => tilemap.GetCellCenterWorld(c);
}
