using UnityEngine;
using System.Collections;

public class SwapInput : MonoBehaviour
{
    public Camera cam;
    public BoardState board;
    public MatchFinder matcher;
    public MatchGameLoop loop;

    [Header("�巡�� ����")]
    public float minDragDistance = 0.5f; // �ּ� �巡�� �Ÿ� (���� ����)

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

        // �巡�� ���� ���� (���� ǥ�� ��)
        //Debug.Log($"�巡�� ����: {dragStartCell}");
    }

    void UpdateDrag()
    {
        // �巡�� �� �ð��� �ǵ�� (������)
        var currentWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        currentWorldPos.z = 0f;

        float dragDistance = Vector3.Distance(dragStartWorldPos, currentWorldPos);
        if (dragDistance >= minDragDistance)
        {
            // �巡�װ� ����� ��� ���� ǥ�� ���� ���� ����
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

        // �ּ� �巡�� �Ÿ� Ȯ��
        if (dragDistance < minDragDistance)
        {
            Debug.Log("�巡�� �Ÿ��� �ʹ� ª���ϴ�");
            ResetDrag();
            return;
        }

        // �巡�� �������� ���� ����� ���� �� ã��
        Vector3 dragDirection = (currentWorldPos - dragStartWorldPos).normalized;
        Vector3Int? targetCell = FindClosestNeighborInDirection(dragStartCell.Value, dragDirection);

        if (targetCell != null && board.pieces.ContainsKey(targetCell.Value))
        {
            Debug.Log($"�巡�� �Ϸ�: {dragStartCell} -> {targetCell}");
            StartCoroutine(TrySwapAndResolve(dragStartCell.Value, targetCell.Value));
        }
        else
        {
            Debug.Log("��ȿ���� ���� �巡�� ���");
        }

        ResetDrag();
    }

    void ResetDrag()
    {
        dragStartCell = null;
        isDragging = false;
        // �巡�� ���� ����
    }

    Vector3Int? FindClosestNeighborInDirection(Vector3Int startCell, Vector3 direction)
    {
        Vector3Int bestNeighbor = startCell;
        float bestDot = -1f;
        bool foundValid = false;

        // 6������ ���� �� �˻�
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

        // ������ ��� ���� ��ġ�ϴ� ��츸 ��ȿ�� ó��
        if (foundValid && bestDot > 0.3f) // ������ �뷫 70�� �̳�
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
        // ����: ��ġ ��ȯ
        var A = board.pieces[a];
        var B = board.pieces[b];
        yield return MoveSwap(A.transform, board.WorldCenter(b), B.transform, board.WorldCenter(a), 0.15f);

        // ���� ����
        board.pieces[a] = B;
        board.pieces[b] = A;
        yield return new WaitForSeconds(0.3f);

        // ��ġ Ȯ��
        var matched = matcher.CollectAllMatches();
        if (matched.Count == 0)
        {
            // �ѹ�
            yield return MoveSwap(A.transform, board.WorldCenter(a), B.transform, board.WorldCenter(b), 0.15f);
            board.pieces[a] = A;
            board.pieces[b] = B;
            yield break;
        }

        // �ؼ� ����
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