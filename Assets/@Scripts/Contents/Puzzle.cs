using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Puzzle : MonoBehaviour
{
    [Header("Type/Visual")]
    public int typeId;                         // 색/종류 구분용 ID
    public SpriteRenderer sr;                  // 스프라이트 렌더러
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
