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

    public event Action<int> OnScoreChanged;
    public event Action<int, Vector3> OnScorePopup; // (�߰�������, ������ġ)
    public event Action<int> OnMovesChanged;                // ���� �̵� ��
    public event Action<int, int> OnObstacleProgressChanged; // (�ı��� ���� ��, ��ǥ)
    public event Action OnStageCleared;                     // �������� Ŭ����

    public int MovesLeft { get; private set; } = 15;
    public int TopGoal { get; private set; } = 5;  // �̹� ���� ��ǥ ���� ��
    public int TopsDestroyed { get; private set; } = 0;

    public int Score { get; private set; }

    Vector3? _taggedPopupWorld; // ���� ���� �˾��� ��� ��ġ(���� ���� �� �±�)

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

    // ===== ���� ����/������Ʈ API =====
    public void BeginLevel(int startMoves, int topGoal)
    {
        Score = 0;

        MovesLeft = Mathf.Max(0, startMoves);
        TopGoal = Mathf.Max(0, topGoal);
        TopsDestroyed = 0;

        // UI �ʱ�ȭ �̺�Ʈ ����
        OnMovesChanged?.Invoke(MovesLeft);
        OnObstacleProgressChanged?.Invoke(TopsDestroyed, TopGoal);
        OnScoreChanged?.Invoke(Score);
    }

    // ������ ���� 1ȸ�� ȣ��
    public void ConsumeMove()
    {
        if (MovesLeft <= 0) return;
        MovesLeft--;
        OnMovesChanged?.Invoke(MovesLeft);
    }

    // ���̰� n�� �ı����� �� ȣ��(���� 1 ���忡 ���� �� ����)
    public void AddTopDestroyed(int n)
    {
        if (n <= 0) return;
        TopsDestroyed += n;
        OnObstacleProgressChanged?.Invoke(TopsDestroyed, TopGoal);

        if (TopGoal > 0 && TopsDestroyed >= TopGoal)
            OnStageCleared?.Invoke();
    }

    // ���� �ı� �� ���� �߰�(�� ���� N*60)
    public void AddScore(int amount)
    {
        if (amount <= 0) return;
        Score += amount;
        OnScoreChanged?.Invoke(Score);

        if (_taggedPopupWorld.HasValue)
        {
            OnScorePopup?.Invoke(amount, _taggedPopupWorld.Value);
            _taggedPopupWorld = null; // �� �� �Һ�
        }
    }

    public void ShowScorePopup(int amount, Vector3 worldPos)
    {
        OnScorePopup?.Invoke(amount, worldPos);
    }

}
