using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Define;
public class MySceneManager
{
    public BaseScene CurrentScene { get { return GameObject.FindObjectOfType<BaseScene>(); } }

    public void LoadScene(EScene type, Transform parents = null)
    {
        switch (CurrentScene.SceneType)
        {
            case EScene.TitleScene:
                SceneManager.LoadScene(GetSceneName(type));
                Managers.Clear();
                break;
            case EScene.GameScene:
                SceneManager.LoadScene(GetSceneName(type));
                Managers.Clear();
                break;
     
        }
    }

    public string GetSceneName(EScene type)
    {
        string name = System.Enum.GetName(typeof(EScene), type);
        return name;
    }

    public void Clear()
    {
        CurrentScene.Clear();
    }

}
