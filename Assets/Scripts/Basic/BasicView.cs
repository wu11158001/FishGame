using UnityEngine;
using System;
using System.Collections;
using UnityEngine.UI;

public abstract class BasicView : MonoBehaviour
{
    [Header("Basic")]
    [SerializeField] protected CanvasGroup MainCanvasGroup;
    [SerializeField] protected Button BgBtn;
    [SerializeField] protected Button CloseBtn;

    protected Action CloseAction;

    protected virtual void Start()
    {
        if (BgBtn != null)
            BgBtn.onClick.AddListener(Close);

        if (CloseBtn != null)
            CloseBtn.onClick.AddListener(Close);
    }

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
