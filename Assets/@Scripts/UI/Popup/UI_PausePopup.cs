using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_PausePopup : UI_Popup
{

    #region Enum
    enum GameObjects
    {
        ContentObject
    }
    enum Buttons
    {
        ContinueButton,
        ReStartButton,
        ExitButton
    }


    #endregion

    private void Awake()
    {
        Init();
    }
    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Object Bind

        BindObject(typeof(GameObjects));
        BindButton(typeof(Buttons));

        GetButton((int)Buttons.ContinueButton).gameObject.BindEvent(OnClickContinueButton);
        GetButton((int)Buttons.ReStartButton).gameObject.BindEvent(OnClickReStartButtonButton);
        GetButton((int)Buttons.ExitButton).gameObject.BindEvent(OnClickExitButton);


        #endregion


        return true;
    }

    private void OnClickReStartButtonButton()
    {
        Managers.Scene.LoadScene(Define.EScene.GameScene);
    }

    private void OnClickContinueButton()
    {
       gameObject.SetActive(false);
    }

    private void OnClickExitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();                              
#endif
    }

    private void OnEnable()
    {
        PopupOpenAnimation(GetObject((int)GameObjects.ContentObject));
    }
}
