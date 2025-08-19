using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class HexBoardSpawner : MonoBehaviour
{
    [Header("Refs")]
    public Tilemap tilemap;            // ����(���) Ÿ���� ĥ���� Tilemap
    public GameObject puzzlePrefab;    // ���� ������(�ǹ� �߾�)
    public Transform pieceParent;      // ������ �θ�(�ɼ�)

    [Header("Timing")]
    public float spawnOffsetY = 4f;    // ����� ���� ����Y + �̸�ŭ ������ ����
    public float fallDuration = 0.35f; // ���� �ð�
    public float interval = 0.06f;     // ���� ���� ����
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private List<Vector3Int> targets = new List<Vector3Int>();
    private Vector3 spawnPos;          // �׻� ���� ���� ��ġ(�� �����)

    void Start()
    {
        BuildTargetsAndSpawnPos();
        StartCoroutine(FillBottomLeftToRight());
    }

    // 1) Ÿ�� ĭ ���� ����� & �� ����� ���� ��ġ ���
    void BuildTargetsAndSpawnPos()
    {
        targets.Clear();
        BoundsInt b = tilemap.cellBounds;

        // ���忡 ĥ���� ��� �� ����
        foreach (var c in b.allPositionsWithin)
            if (tilemap.HasTile(c))
                targets.Add(c);

        if (targets.Count == 0)
        {
            Debug.LogWarning("No tiles on tilemap.");
            return;
        }

        // Ÿ�� ����: "�Ʒ� �� �� �� ��" & "���� �� �ȿ����� �ޡ��"
        // ��翡���� cell.y(��), cell.x(��) �����̸� �ڿ�������
        targets = targets
            .OrderBy(c => c.y)   // �Ʒ��� ����
            .ThenBy(c => c.x)    // ���� ���̸� ������ ����
            .ToList();

        // ���� ��ġ: ���� �ֻ��(���� ū y)�� �� �߽� x�� ��������, �� ���� spawnOffsetY
        var topCell = targets.OrderByDescending(c => c.y).First();
        Vector3 topWorld = tilemap.GetCellCenterWorld(topCell);
        spawnPos = new Vector3(topWorld.x, topWorld.y + spawnOffsetY, topWorld.z);
    }

    // 2) ����� �� �������� �ϳ��� ����߷� ������� ä���
    IEnumerator FillBottomLeftToRight()
    {
        for (int i = 0; i < targets.Count && i < 30; i++)
        {
            Vector3Int cell = targets[i];
            Vector3 targetWorld = tilemap.GetCellCenterWorld(cell);

            var go = Instantiate(
                puzzlePrefab,
                spawnPos,
                Quaternion.identity,
                pieceParent != null ? pieceParent : transform
            );

            yield return StartCoroutine(FallLerp(go.transform, spawnPos, targetWorld, fallDuration));
            yield return new WaitForSeconds(interval);
        }
    }

    // ���� ����(�߷�ó�� ���̰�)
    IEnumerator FallLerp(Transform t, Vector3 from, Vector3 to, float dur)
    {
        float e = 0f;
        while (e < dur)
        {
            float k = ease.Evaluate(e / dur);        // EaseOut �迭 ��õ
            t.position = Vector3.LerpUnclamped(from, to, k);
            e += Time.deltaTime;
            yield return null;
        }
        t.position = to;
    }
}
