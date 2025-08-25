using UnityEngine;
using System.Collections;

public class SwapInput : MonoBehaviour
{
    public Camera cam;
    public BoardState board;
    public MatchFinder matcher;
    public MatchGameLoop loop;

    [Header("드래그 설정")]
    public float minDragDistance = 0.5f; // 최소 드래그 거리 (월드 단위)

    Vector3Int? dragStartCell;
    Vector3 dragStartWorldPos;
    bool isDragging = false;

    void Update()
    {
        HandleDragInput();
    }

    void HandleDragInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartDrag();
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            UpdateDrag();
        }
        else if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDrag();
        }
    }

    void StartDrag()
    {
        var cell = PickCell(Input.mousePosition);
        if (cell == null || !board.pieces.ContainsKey(cell.Value)) return;

        dragStartCell = cell;
        dragStartWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        dragStartWorldPos.z = 0f;
        isDragging = true;

        // 드래그 시작 연출 (선택 표시 등)
        //Debug.Log($"드래그 시작: {dragStartCell}");
    }

    void UpdateDrag()
    {
        // 드래그 중 시각적 피드백 (선택적)
        var currentWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        currentWorldPos.z = 0f;

        float dragDistance = Vector3.Distance(dragStartWorldPos, currentWorldPos);
        if (dragDistance >= minDragDistance)
        {
            // 드래그가 충분히 길면 방향 표시 등의 연출 가능
        }
    }

    void EndDrag()
    {
        if (dragStartCell == null)
        {
            isDragging = false;
            return;
        }

        var currentWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        currentWorldPos.z = 0f;

        float dragDistance = Vector3.Distance(dragStartWorldPos, currentWorldPos);

        // 최소 드래그 거리 확인
        if (dragDistance < minDragDistance)
        {
            Debug.Log("드래그 거리가 너무 짧습니다");
            ResetDrag();
            return;
        }

        // 드래그 방향으로 가장 가까운 인접 셀 찾기
        Vector3 dragDirection = (currentWorldPos - dragStartWorldPos).normalized;
        Vector3Int? targetCell = FindClosestNeighborInDirection(dragStartCell.Value, dragDirection);

        if (targetCell != null && board.pieces.ContainsKey(targetCell.Value))
        {
            Debug.Log($"드래그 완료: {dragStartCell} -> {targetCell}");
            StartCoroutine(TrySwapAndResolve(dragStartCell.Value, targetCell.Value));
        }
        else
        {
            Debug.Log("유효하지 않은 드래그 대상");
        }

        ResetDrag();
    }

    void ResetDrag()
    {
        dragStartCell = null;
        isDragging = false;
        // 드래그 연출 정리
    }

    Vector3Int? FindClosestNeighborInDirection(Vector3Int startCell, Vector3 direction)
    {
        Vector3Int bestNeighbor = startCell;
        float bestDot = -1f;
        bool foundValid = false;

        // 6방향의 인접 셀 검사
        for (int i = 0; i < 6; i++)
        {
            Vector3Int neighbor = PuzzleDirs.Step(startCell, i);
            if (!board.IsValidCell(neighbor)) continue;

            Vector3 neighborWorldPos = board.WorldCenter(neighbor);
            Vector3 startWorldPos = board.WorldCenter(startCell);
            Vector3 neighborDirection = (neighborWorldPos - startWorldPos).normalized;

            float dot = Vector3.Dot(direction, neighborDirection);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestNeighbor = neighbor;
                foundValid = true;
            }
        }

        // 방향이 어느 정도 일치하는 경우만 유효로 처리
        if (foundValid && bestDot > 0.3f) // 각도가 대략 70도 이내
        {
            return bestNeighbor;
        }

        return null;
    }

    Vector3Int? PickCell(Vector3 screenPos)
    {
        var world = cam.ScreenToWorldPoint(screenPos);
        world.z = 0f;
        var cell = board.tilemap.WorldToCell(world);
        if (!board.IsValidCell(cell)) return null;
        return cell;
    }

    IEnumerator TrySwapAndResolve(Vector3Int a, Vector3Int b)
    {
        // 연출: 위치 교환
        var A = board.pieces[a];
        var B = board.pieces[b];
        yield return MoveSwap(A.transform, board.WorldCenter(b), B.transform, board.WorldCenter(a), 0.15f);

        // 상태 스왑
        board.pieces[a] = B;
        board.pieces[b] = A;
        yield return new WaitForSeconds(0.3f);

        // 매치 확인
        var matched = matcher.CollectAllMatches();
        if (matched.Count == 0)
        {
            // 롤백
            yield return MoveSwap(A.transform, board.WorldCenter(a), B.transform, board.WorldCenter(b), 0.15f);
            board.pieces[a] = A;
            board.pieces[b] = B;
            yield break;
        }

        // 해소 루프
        Managers.Game.ConsumeMove();
        yield return loop.ResolveAllMatchesThenIdle();
    }

    IEnumerator MoveSwap(Transform A, Vector3 toA, Transform B, Vector3 toB, float dur)
    {
        float e = 0f;
        var a0 = A.position;
        var b0 = B.position;

        while (e < dur)
        {
            float t = e / dur;
            A.position = Vector3.LerpUnclamped(a0, toA, t);
            B.position = Vector3.LerpUnclamped(b0, toB, t);
            e += Time.deltaTime;
            yield return null;
        }

        A.position = toA;
        B.position = toB;
    }
}