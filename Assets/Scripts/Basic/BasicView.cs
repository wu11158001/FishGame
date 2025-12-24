using UnityEngine;
using System;

public abstract class BasicView : MonoBehaviour
{
    protected Action CloseAction;

    /// <summary>
    /// 關閉介面
    /// </summary>
    protected virtual void Close()
    {
        CloseAction?.Invoke();
    }
}
