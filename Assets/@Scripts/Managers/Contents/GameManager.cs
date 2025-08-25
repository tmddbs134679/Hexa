using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using static Define;

[Serializable]
public class GameData
{

    public bool BGMOn = true;
    public bool EffectSoundOn = true;
}

public class GameManager : MonoBehaviour
{
    public GameData _gameData = new GameData();


    public event Action<int> OnMovesChanged;                // 남은 이동 수
    public event Action<int, int> OnObstacleProgressChanged; // (파괴한 팽이 수, 목표)
    public event Action OnStageCleared;                     // 스테이지 클리어

    public int MovesLeft { get; private set; } = 15;
    public int TopGoal { get; private set; } = 5;  // 이번 레벨 목표 팽이 수
    public int TopsDestroyed { get; private set; } = 0;

    #region Option
    public bool BGMOn
    {
        get { return _gameData.BGMOn; }
        set
        {
            if (_gameData.BGMOn == value)
                return;
            _gameData.BGMOn = value;
            if (_gameData.BGMOn == false)
            {
                Managers.Sound.Stop(ESound.Bgm);
            }
            else
            {
                string name = "BGM1";
                if (Managers.Scene.CurrentScene.SceneType == Define.EScene.GameScene)
                    name = "BGM1";

                Managers.Sound.Play(Define.ESound.Bgm, name);
            }
        }
    }

    public bool EffectSoundOn
    {
        get { return _gameData.EffectSoundOn; }
        set { _gameData.EffectSoundOn = value; }
    }



    #endregion

    public void Init()
    {

    }

    // ===== 레벨 시작/업데이트 API =====
    public void BeginLevel(int startMoves, int topGoal)
    {
        MovesLeft = Mathf.Max(0, startMoves);
        TopGoal = Mathf.Max(0, topGoal);
        TopsDestroyed = 0;

        // UI 초기화 이벤트 발행
        OnMovesChanged?.Invoke(MovesLeft);
        OnObstacleProgressChanged?.Invoke(TopsDestroyed, TopGoal);
    }

    // 성공한 스왑 1회당 호출
    public void ConsumeMove()
    {
        if (MovesLeft <= 0) return;
        MovesLeft--;
        OnMovesChanged?.Invoke(MovesLeft);
    }

    // 팽이가 n개 파괴됐을 때 호출(보통 1 라운드에 여러 개 가능)
    public void AddTopDestroyed(int n)
    {
        if (n <= 0) return;
        TopsDestroyed += n;
        OnObstacleProgressChanged?.Invoke(TopsDestroyed, TopGoal);

        if (TopGoal > 0 && TopsDestroyed >= TopGoal)
            OnStageCleared?.Invoke();
    }


}
