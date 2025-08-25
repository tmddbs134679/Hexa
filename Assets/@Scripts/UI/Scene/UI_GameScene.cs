using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_GameScene : UI_Scene
{

    #region Enum
    enum GameObjects
    {
        TopMenu
    }

    enum Buttons
    {
        PauseButton
    }

    enum Texts
    {
        ObstacleText,
        MoveCountText,
        ScoreText
    }
    #endregion
    public Canvas mainCanvas;                 // �ν����Ϳ��� ����(Overlay�� WorldCamera ����� ��)
    public GameObject scoreFloaterPrefab;
    [SerializeField] private UI_ClearPopup _clearPopup;
    [SerializeField] private UI_PausePopup _PausePopup;
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
        _PausePopup.gameObject.SetActive(false);
        Refresh();

        GetButton((int)Buttons.PauseButton).gameObject.BindEvent(OnClickPauseButton);
        return true;
    }

    private void OnClickPauseButton()
    {
        _PausePopup.gameObject.SetActive(true);
    }

    private void Awake()
    {
        Init();
    }

    private void Start()
    {
    
    }
    void OnEnable()
    {
        Managers.Game.OnMovesChanged += HandleMovesChanged;
        Managers.Game.OnObstacleProgressChanged += HandleObstacleChanged;
        Managers.Game.OnStageCleared += HandleStageCleared;
        Managers.Game.OnScoreChanged += HandleScoreChanged;
        Managers.Game.OnScorePopup += HandleScorePopup;
    }

    void OnDisable()
    {
        Managers.Game.OnMovesChanged -= HandleMovesChanged;
        Managers.Game.OnObstacleProgressChanged -= HandleObstacleChanged;
        Managers.Game.OnStageCleared -= HandleStageCleared;
        Managers.Game.OnScoreChanged -= HandleScoreChanged;
        Managers.Game.OnScorePopup -= HandleScorePopup;
    }

    private void Refresh()
    {
        GetText((int)Texts.ObstacleText).text = Managers.Game.TopGoal.ToString();
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
        // TODO: Ŭ���� �˾�/����

        StartCoroutine(OpenClearPopupNextFrame());
    }

    IEnumerator OpenClearPopupNextFrame()
    {
        yield return null; // �� ������ �纸 �� ���� �ؽ�Ʈ ���� ��� UI ������Ʈ �Ϸ�
        _clearPopup.gameObject.SetActive(true);
    }

    void HandleScoreChanged(int total)
    {
        var t = GetText((int)Texts.ScoreText);
        if (t != null) t.text = total.ToString();
    }

    void HandleScorePopup(int added, Vector3 worldPos)
    {
        SpawnScorePopupWS(scoreFloaterPrefab, worldPos, added);
    }

    public void SpawnScorePopupWS(GameObject prefab, Vector3 worldPos, int amount)
    {
        var go = Instantiate(prefab);
        go.GetComponent<Canvas>().sortingOrder = 100;
        go.transform.position = worldPos;

        var txt = go.GetComponentInChildren<TMP_Text>(); // UI.Text�� Ÿ�� ����
        if (txt) txt.text = $"{amount}";

        var cg = go.GetComponent<CanvasGroup>();
        if (cg) cg.alpha = 1f;

        // ���� ��¦ �̵� + ���̵�ƿ�
        go.transform.DOMoveY(worldPos.y + 0.8f, 0.6f).SetEase(Ease.OutQuad);
        if (cg) cg.DOFade(0f, 0.6f).SetEase(Ease.InQuad)
               .OnComplete(() => Destroy(go));
    }

}
