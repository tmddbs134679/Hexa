using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using DG.Tweening;

/*
 ��ġ-3(���) ���� ������ �ڷ�ƾ���� ����: �ʱ� ��ġ �� ��ġ ���� �� ����/���� �� �ٽ� ��ġ�� �� **����(cascade)**�� ���� ������ �ݺ�.
 */
public class MatchGameLoop : MonoBehaviour
{
    [Header("Refs")]
    public BoardState board;
    public TopFiller filler;
    public GravityWithSlide gravity;
    public MatchFinder matcher;

    [Header("Init")]
    public int initialCount = 1;

    [Header("Animation Settings")]
    public float destroyAnimDuration = 0.3f;
    public float destroyScaleMultiplier = 1.5f;
    public bool useSequentialDestroy = false;
    public float sequentialDelay = 0.05f;

    void Start()
    {
        StartCoroutine(GameStartRoutine());
    }

    IEnumerator GameStartRoutine()
    {
        // �������ڸ��� 30ĭ ���� �� ä�� ���·� ����
        yield return StartCoroutine(filler.SpawnSequence(30, 0.05f, 0.12f));
        //StartCoroutine(ResolveAllMatchesThenIdle());
        // �ʿ��ϸ� �ʹݺ��� ��ġ ���� ���� ������ ���� ���� �Ʒ� �� �� ����
        yield return ResolveAllMatchesThenIdle();
        yield break;
    }

    public IEnumerator ResolveAllMatchesThenIdle()
    {
        while (true)
        {
            var matched = matcher.CollectAllMatches();
            if (matched.Count == 0)
                yield break;

            // ��ġ�� �������� �ı� �ִϸ��̼�
            yield return StartCoroutine(DestroyMatchedPiecesWithAnimation(matched));

            // ���� ������ŭ ��� ���� + ���� ���� ó��
            yield return StartCoroutine(gravity.ApplyWithSpawn(matched.Count));
        }
    }

    IEnumerator DestroyMatchedPiecesWithAnimation(HashSet<Vector3Int> matchedCells)
    {
        List<Transform> matchedTransforms = new List<Transform>();

        // ��ġ�� �������� Transform ����
        foreach (var cell in matchedCells)
        {
            if (board.pieces.TryGetValue(cell, out var go) && go != null)
            {
                matchedTransforms.Add(go.transform);
            }
        }

        if (matchedTransforms.Count == 0)
        {
            // Transform�� ������ �׳� ���忡���� ����
            foreach (var cell in matchedCells)
            {
                board.pieces.Remove(cell);
            }
            yield break;
        }

        if (useSequentialDestroy)
        {
            // ������ �ı� �ִϸ��̼�
            yield return StartCoroutine(DestroySequentially(matchedTransforms, matchedCells));
        }
        else
        {
            // ���� �ı� �ִϸ��̼�
            yield return StartCoroutine(DestroySimultaneously(matchedTransforms, matchedCells));
        }
    }

    IEnumerator DestroySimultaneously(List<Transform> transforms, HashSet<Vector3Int> cells)
    {
        // ���ÿ� ��� ���� �ִϸ��̼� ����
        foreach (var transform in transforms)
        {
            if (transform != null)
            {
                AnimatePieceDestroy(transform, 0f);
            }
        }

        // �ִϸ��̼� �Ϸ���� ���
        yield return new WaitForSeconds(destroyAnimDuration);

        // ���� ������Ʈ ���� �� ���忡�� ����
        CleanupPieces(transforms, cells);
    }

    IEnumerator DestroySequentially(List<Transform> transforms, HashSet<Vector3Int> cells)
    {
        // ���������� �ִϸ��̼� ����
        for (int i = 0; i < transforms.Count; i++)
        {
            var transform = transforms[i];
            if (transform != null)
            {
                float delay = i * sequentialDelay;
                AnimatePieceDestroy(transform, delay);
            }
        }

        // ��� �ִϸ��̼� �Ϸ� ��� (������ ������ ���� �ð� + �ִϸ��̼� �ð�)
        float totalTime = (transforms.Count - 1) * sequentialDelay + destroyAnimDuration;
        yield return new WaitForSeconds(totalTime);

        // ����
        CleanupPieces(transforms, cells);
    }

    void AnimatePieceDestroy(Transform transform, float delay)
    {
        if (transform == null) return;

        // ũ�� Ȯ�� + ȸ��
        transform.DOScale(Vector3.one * destroyScaleMultiplier, destroyAnimDuration)
                 .SetDelay(delay)
                 .SetEase(Ease.OutBack);

        transform.DORotate(new Vector3(0, 0, 360), destroyAnimDuration, RotateMode.FastBeyond360)
                 .SetDelay(delay)
                 .SetEase(Ease.InOutQuad);

        // ���̵� �ƿ�
        var spriteRenderer = transform.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.DOFade(0f, destroyAnimDuration * 0.7f)
                          .SetDelay(delay + destroyAnimDuration * 0.3f);
        }

        // CanvasGroup�� �ִٸ� (UI ������)
        var canvasGroup = transform.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, destroyAnimDuration * 0.7f)
                       .SetDelay(delay + destroyAnimDuration * 0.3f);
        }
    }

    void CleanupPieces(List<Transform> transforms, HashSet<Vector3Int> cells)
    {
        // DoTween ���� �� ������Ʈ �ı�
        foreach (var transform in transforms)
        {
            if (transform != null)
            {
                transform.DOKill(); // �޸� ���� ����
                Destroy(transform.gameObject);
            }
        }

        // ���忡�� ����
        foreach (var cell in cells)
        {
            board.pieces.Remove(cell);
        }
    }
}