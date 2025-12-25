using UnityEngine;
using System;
using System.Collections;

public abstract class BasicView : MonoBehaviour
{
    [Header("Basic")]
    [SerializeField] protected CanvasGroup MainCanvasGroup;

    [Space(30)]

    protected Action CloseAction;

    /// <summary>
    /// 關閉介面
    /// </summary>
    protected virtual void Close()
    {
        CloseAction?.Invoke();
    }

    /// <summary>
    /// 等待介面排版在顯示
    /// </summary>
    /// <returns></returns>
    protected IEnumerator IYieldShow()
    {
        yield return null;
        yield return null;
        yield return null;

        if (MainCanvasGroup != null)
            MainCanvasGroup.alpha = 1;
    }
}
