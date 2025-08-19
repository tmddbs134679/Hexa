using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Toast : UI_Popup
{
    #region Enum

    enum Buttons
    {
        BackgroundButton
    }

    enum Texts
    {
        ToastMessageValueText,
    }
    public void OnEnable()
    {
        // Tm 오픈 연출
        PopupOpenAnimation(gameObject);
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
        BindButton(typeof(Buttons));
        BindText(typeof(Texts));

        #endregion

        GetButton((int)Buttons.BackgroundButton).gameObject.BindEvent(OnClickBackgroundButton);


        Refresh();
        return true;
    }

    private void OnClickBackgroundButton()
    {
        gameObject.SetActive(false);
    }

    public void SetInfo(string msg)
    {
        // 메시지 변경
        transform.localScale = Vector3.one;
        GetText((int)Texts.ToastMessageValueText).text = msg;
        Refresh();
    }

    void Refresh()
    {


    }

}
