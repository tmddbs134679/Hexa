using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Puzzle : MonoBehaviour
{
    [Header("Type/Visual")]
    public int typeId;                         // 매치 색/종류 구분용 ID
    public SpriteRenderer sr;                  // 스프라이트 렌더러

    // === 능력 플래그(상속으로 세부 오브젝트가 오버라이드) ===
    public virtual bool IsMatchable => true;        // 매치 탐색에 참여?
    public virtual bool IsSwappable => true;        // 드래그 스왑 가능?
    public virtual bool AffectedByGravity => true;  // 중력 이동 대상?

    void Awake()
    {
        if (sr == null)
            sr = GetComponent<SpriteRenderer>();
    }

    public void SetType(int id, Sprite sprite)
    {
        typeId = id;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = sprite;
    }
}
