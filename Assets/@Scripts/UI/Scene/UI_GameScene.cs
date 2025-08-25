using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_GameScene : UI_Scene
{

    #region Enum
    enum GameObjects
    {
        TopMenu
    }

    enum Buttons
    {

    }

    enum Texts
    {
        ObstacleText,
        MoveCountText
    }
    #endregion

    [SerializeField] private UI_ClearPopup _clearPopup;
    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Object Bind

        BindObject(typeof(GameObjects));
        BindButton(typeof(Buttons));
        BindText(typeof(Texts));

        #endregion

        _clearPopup.gameObject.SetActive(false);

        return true;
    }

 

    private void Awake()
    {
        Init();
    }

    private void Start()
    {
        Refresh();
    }
    void OnEnable()
    {
        Managers.Game.OnMovesChanged += HandleMovesChanged;
        Managers.Game.OnObstacleProgressChanged += HandleObstacleChanged;
        Managers.Game.OnStageCleared += HandleStageCleared;
    }

    void OnDisable()
    {
        Managers.Game.OnMovesChanged -= HandleMovesChanged;
        Managers.Game.OnObstacleProgressChanged -= HandleObstacleChanged;
        Managers.Game.OnStageCleared -= HandleStageCleared;
    }

    private void Refresh()
    {
        GetText((int)Texts.ObstacleText).text = Managers.Game.TopGoal.ToString();
        GetText((int)Texts.MoveCountText).text = Managers.Game.MovesLeft.ToString();
    }

    void HandleMovesChanged(int left)
    {
        var t = GetText((int)Texts.MoveCountText);

        if (t != null)
            t.text = left.ToString();
    }

    void HandleObstacleChanged(int destroyed, int goal)
    {
        var t = GetText((int)Texts.ObstacleText);

        if (t != null)
        {
            int count = goal - destroyed;
            if (count == 0)
                t.text = "V";
            else
                t.text = count.ToString();


        }
            
    }

    void HandleStageCleared()
    {
        // TODO: 클리어 팝업/연출

        _clearPopup.gameObject.SetActive(true);
    }
}
