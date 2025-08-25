using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class UI_TitleScene : UI_Scene
{
    #region Enum
    enum GameObjects
    {

    }

    enum Buttons
    {
        StartButton
    }

    enum Texts
    {
        StartText
    }
    #endregion

    bool isPreload = false;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;
        // 오브젝트 바인딩
        BindObject(typeof(GameObjects));
        BindButton(typeof(Buttons));
        BindText(typeof(Texts));

        GetButton((int)Buttons.StartButton).gameObject.BindEvent(() =>
        {
            Managers.Scene.LoadScene(Define.EScene.GameScene, transform);
        });

        return true;
    }

    private void Awake()
    {
        Init();
    }
    private void Start()
    {
        Managers.Game.Init();
        StartButtonAnimation();
    }

    void StartButtonAnimation()
    {
        GetText((int)Texts.StartText).DOFade(0, 1f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutCubic).Play();
    }

}
