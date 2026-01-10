using UnityEngine;
using System;
using System.Collections;
using UnityEngine.UI;
using DG.Tweening;

public abstract class BasicView : MonoBehaviour
{
    [Header("Basic")]
    [SerializeField] protected CanvasGroup MainCanvasGroup;
    [SerializeField] protected Button BgBtn;
    [SerializeField] protected Button CloseBtn;

    [Header("Basic PopUp")]
    [SerializeField] protected bool IsUsePopUp;
    [SerializeField] protected RectTransform PopUpRect;
    [SerializeField] protected float PopUpTime = 0.5f;

    protected Action CloseAction;

    protected virtual void Start()
    {
        if (BgBtn != null)
            BgBtn.onClick.AddListener(Close);

        if (CloseBtn != null)
            CloseBtn.onClick.AddListener(Close);
    }

    protected virtual void OnEnable()
    {
        if (IsUsePopUp)
            PopUpEffect();
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

    /// <summary>
    /// 由下彈出效果
    /// </summary>
    protected void PopUpEffect()
    {
        if(PopUpRect != null)
        {
            PopUpRect.anchoredPosition = new(0, -AddressableManagement.Instance.TargetResolution.y);
            PopUpRect.DOAnchorPos(Vector2.zero, PopUpTime).SetEase(Ease.OutBack);
        }
    }
}
