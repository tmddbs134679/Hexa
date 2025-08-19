
using UnityEngine;
using static Define;
public abstract class BaseObject : MonoBehaviour
{
    public EObjectType ObjectType { get; protected set; }

    bool _init = false;
    void Awake()
    {
        Init();
    }

    public virtual bool Init()
    {
        if (_init)
            return false;

        _init = true;
        return true;
    }

    public abstract void OnClick();

}
