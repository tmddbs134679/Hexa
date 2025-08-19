using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TopFiller : MonoBehaviour
{
    public BoardState board;
    public GameObject puzzlePrefab;
    public Transform pieceParent;

    [Header("Spawn")]
    public Vector3Int spawnCell;     // 정확한 스폰 셀(인스펙터에서 지정)
    public float spawnOffsetY = 4f;  // 스폰 월드 위치를 셀 중심에서 위로 올림
    Vector3 spawnWorldPos;

    [Header("Anim")]
    public float fallDuration = 0.35f;
    public float interval = 0.06f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Types")]
    public Sprite[] typeSprites;
    public int typeCount = 5;

    public bool IsRunning { get; private set; }

    void Awake()
    {
        var center = board.WorldCenter(spawnCell);
        spawnWorldPos = new Vector3(center.x, center.y + spawnOffsetY, center.z);
    }

    /// <summary>초기 세팅 등 "특정 칸들"을 위에서 떨어뜨려 채울 때 사용</summary>
    public void FillCells(IEnumerable<Vector3Int> cells)
    {
        StartCoroutine(FillRoutine(cells));
    }

    IEnumerator FillRoutine(IEnumerable<Vector3Int> targetCells)
    {
        if (IsRunning) yield break;
        IsRunning = true;

        var ordered = targetCells.OrderBy(c => c.y).ThenBy(c => c.x).ToList();

        foreach (var cell in ordered)
        {
            if (board.pieces.ContainsKey(cell)) continue;

            var go = SpawnOne();
            yield return Fall(go.transform, board.WorldCenter(cell), fallDuration);
            board.pieces[cell] = go;

            yield return new WaitForSeconds(interval);
        }

        IsRunning = false;
    }

    /// <summary>
    /// 스폰 큐 주입용: 스폰 월드 위치에 퍼즐 프리팹을 생성해 반환(보드 등록은 하지 않음)
    /// </summary>
    public GameObject SpawnOne()
    {
        var go = Instantiate(puzzlePrefab, spawnWorldPos, Quaternion.identity,
                             pieceParent ? pieceParent : transform);

        var piece = go.GetComponent<Puzzle>();
        int tid = Random.Range(0, Mathf.Max(1, typeCount));
        var sprite = (typeSprites != null && tid < typeSprites.Length) ? typeSprites[tid] : null;
        piece.SetType(tid, sprite);
        return go;
    }

    IEnumerator Fall(Transform t, Vector3 target, float dur)
    {
        var from = t.position; float e = 0f;
        while (e < dur)
        {
            float k = ease.Evaluate(e / dur);
            t.position = Vector3.LerpUnclamped(from, target, k);
            e += Time.deltaTime; yield return null;
        }
        t.position = target;
    }
}
