using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameScene : BaseScene
{
    private void Awake()
    {
        Init();
    }

    protected override void Init()
    {

        SceneType = Define.EScene.GameScene;
    

    }

    public override void Clear()
    {
     


    }
}
