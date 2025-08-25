using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_ClearPopup : UI_Popup
{

    #region Enum
    enum GameObjects
    {
        ContentObject
    }
    enum Buttons
    {
        ExitButton
    }
    enum Texts
    {
        ClearText
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
        BindText(typeof(Texts));

        GetButton((int)Buttons.ExitButton).gameObject.BindEvent(OnClickExitButton);


        #endregion


        return true;
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
