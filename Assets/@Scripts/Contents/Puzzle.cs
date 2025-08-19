using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Puzzle : MonoBehaviour
{
    [Header("Type/Visual")]
    public int typeId;                         // ��/���� ���п� ID
    public SpriteRenderer sr;                  // ��������Ʈ ������
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
