using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TextCore.Text;

public class ObjectManager
{

    public Transform CharacterTransform
    {
        get
        {
            GameObject root = GameObject.Find("Character");
            if (root == null)
                root = new GameObject { name = "Character" };
            return root.transform;
        }
    }

    public Transform BuildingTransform
    {
        get
        {
            GameObject root = GameObject.Find("Building");
            if (root == null)
                root = new GameObject { name = "Building" };
            return root.transform;
        }
    }

    public ObjectManager()
    {
        Init();
    }

    private void Init()
    {

    }

    public void Clear()
    {

    }

    public T Spawn<T>(Vector3 position, string templateID, Transform parent = null, bool isReplica = false, string prefabName = "") where T : BaseObject
    {
        return null;


    }

}
