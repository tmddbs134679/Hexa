

using System;
using UnityEngine.EventSystems;
using UnityEngine;

public static class Extension
{
    public static void BindEvent(this GameObject go, Action action = null, Action<BaseEventData> dragAction = null, Define.EUIEvent type = Define.EUIEvent.Click)
    {
        UI_Base.BindEvent(go, action, dragAction, type);
    }


    public static void DestroyChilds(this GameObject go)
    {
        Transform[] children = new Transform[go.transform.childCount];
        for (int i = 0; i < go.transform.childCount; i++)
        {
            children[i] = go.transform.GetChild(i);
        }

        // ��� �ڽ� ������Ʈ ����
        foreach (Transform child in children)
        {
            Managers.Resource.Destroy(child.gameObject);
        }
    }

}
