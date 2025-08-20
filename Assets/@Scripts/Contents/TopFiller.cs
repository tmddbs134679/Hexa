using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/*
 * TopFiller — "스폰 전담"
 * - 퍼즐 프리팹을 스폰셀 위쪽(world pos)에 생성 (SpawnOne)
 * - 채우고 싶은 셀 목록/개수만 받아서, 실제 배치는 GravityWithSlide에 위임
 * - 보드 등록/낙하/슬라이드는 절대 하지 않음
 */

public class TopFiller : MonoBehaviour
{
    [Header("Refs")]
    public BoardState board;
    public GravityWithSlide gravity;   // ★ 반드시 연결
    public GameObject puzzlePrefab;
    public Transform pieceParent;

    [Header("Spawn")]
    public Vector3Int spawnCell;       // 스폰셀(그리드 좌표)
    public float spawnOffsetY = 4f;    // 스폰 월드 위치를 셀 중심에서 위로 올림
    private Vector3 spawnWorldPos;

    [Header("Types")]
    public Sprite[] typeSprites;
    public int typeCount = 5;

    public bool IsRunning { get; private set; }

    void Awake()
    {
        // 자동 바인딩(있으면 쓰고, 없으면 찾아봄)
        if (!board) board = GetComponentInParent<BoardState>() ?? FindObjectOfType<BoardState>();
        if (!gravity) gravity = GetComponentInParent<GravityWithSlide>() ?? FindObjectOfType<GravityWithSlide>();

        if (!board) { Debug.LogError("[TopFiller] BoardState missing"); enabled = false; return; }
        if (!gravity) { Debug.LogError("[TopFiller] Gravity missing"); enabled = false; return; }

        var center = board.WorldCenter(spawnCell);
        spawnWorldPos = new Vector3(center.x, center.y + spawnOffsetY, center.z);
    }

    /// <summary>목표 셀들을 중력 규칙으로 채움(비어있는 칸 개수만큼만 보충)</summary>
    public void FillCells(IEnumerable<Vector3Int> targetCells)
    {
        StartCoroutine(FillRoutine_ByTargets(targetCells));
    }

    /// <summary>개수만큼 중력 규칙으로 보충(초기화 편의용)</summary>
    public void FillCount(int count)
    {
        StartCoroutine(FillRoutine_ByCount(count));
    }


    public IEnumerator SpawnSequence(int count, float minDelay = 0.0f, float maxDelay = 0.0f)
    {
        if (IsRunning) yield break;
        IsRunning = true;

        int left = Mathf.Max(0, count);
        while (left-- > 0)
        {
            // 한 번에 1개만 주입 → 낙하/슬라이드 끝날 때까지 대기
            yield return StartCoroutine(gravity.ApplyWithSpawn(1));

            // 살짝 템포 주고 싶으면
            if (maxDelay > 0f)
                yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
        }

        IsRunning = false;
    }

    private IEnumerator FillRoutine_ByTargets(IEnumerable<Vector3Int> targetCells)
    {
        if (IsRunning) yield break;
        IsRunning = true;

        if (targetCells == null)
        {
            Debug.LogError("[TopFiller] targetCells is null");
            IsRunning = false; yield break;
        }

        var targets = targetCells as IList<Vector3Int> ?? targetCells.ToList();
        int need = targets.Count(c => board.IsValidCell(c) && board.IsEmpty(c));

        if (need > 0)
            yield return StartCoroutine(gravity.ApplyWithSpawn(need));

        IsRunning = false;
    }

    private IEnumerator FillRoutine_ByCount(int count)
    {
        if (IsRunning) yield break;
        IsRunning = true;

        if (count > 0)
            yield return StartCoroutine(gravity.ApplyWithSpawn(count));

        IsRunning = false;
    }

    /// <summary>GravityWithSlide에서 스폰 큐에 넣을 때 호출</summary>
    public GameObject SpawnOne()
    {
        var go = Instantiate(
            puzzlePrefab,
            spawnWorldPos,
            Quaternion.identity,
            pieceParent ? pieceParent : transform
        );

        var piece = go.GetComponent<Puzzle>();
        int tid = Random.Range(0, Mathf.Max(1, typeCount));
        var sprite = (typeSprites != null && tid < typeSprites.Length) ? typeSprites[tid] : null;
        piece.SetType(tid, sprite);
        return go;
    }
}
