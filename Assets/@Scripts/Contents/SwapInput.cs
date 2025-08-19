//using UnityEngine;
//using System.Collections;

//public class SwapInput : MonoBehaviour
//{
//    public Camera cam;
//    public BoardState board;
//    public MatchFinder matcher;
//    public MatchGameLoop loop;

//    Vector3Int? selected;

//    void Update()
//    {
//        if (Input.GetMouseButtonDown(0))
//        {
//            var cell = PickCell(Input.mousePosition);
//            if (cell == null || !board.pieces.ContainsKey(cell.Value)) return;

//            if (selected == null)
//            {
//                selected = cell;
//                // 선택 연출 가능
//            }
//            else
//            {
//                var a = selected.Value;
//                var b = cell.Value;
//                selected = null;

//                if (AreNeighbors(a, b))
//                    StartCoroutine(TrySwapAndResolve(a, b));
//            }
//        }
//    }

//    Vector3Int? PickCell(Vector3 screenPos)
//    {
//        var world = cam.ScreenToWorldPoint(screenPos);
//        world.z = 0f;
//        var cell = board.tilemap.WorldToCell(world);
//        if (!board.IsValidCell(cell)) return null;
//        return cell;
//    }

//    bool AreNeighbors(Vector3Int a, Vector3Int b)
//    {
//        for (int i = 0; i < 6; i++)
//            if (PuzzleDirs.Step(a, i) == b) return true;
//        return false;
//    }

//    IEnumerator TrySwapAndResolve(Vector3Int a, Vector3Int b)
//    {
//        // 연출: 위치 교환
//        var A = board.pieces[a];
//        var B = board.pieces[b];
//        yield return MoveSwap(A.transform, board.WorldCenter(b), B.transform, board.WorldCenter(a), 0.15f);

//        // 상태 스왑
//        board.pieces[a] = B;
//        board.pieces[b] = A;

//        // 매치 확인
//        var matched = matcher.CollectMatchesFrom(new[] { a, b });
//        if (matched.Count == 0)
//        {
//            // 롤백
//            yield return MoveSwap(B.transform, board.WorldCenter(a), A.transform, board.WorldCenter(b), 0.15f);
//            board.pieces[a] = A;
//            board.pieces[b] = B;
//            yield break;
//        }

//        // 해소 루프
//        yield return loop.ResolveAllMatchesThenIdle();
//    }

//    IEnumerator MoveSwap(Transform A, Vector3 toA, Transform B, Vector3 toB, float dur)
//    {
//        float e = 0f; var a0 = A.position; var b0 = B.position;
//        while (e < dur)
//        {
//            float t = e / dur;
//            A.position = Vector3.LerpUnclamped(a0, toA, t);
//            B.position = Vector3.LerpUnclamped(b0, toB, t);
//            e += Time.deltaTime; yield return null;
//        }
//        A.position = toA; B.position = toB;
//    }
//}
