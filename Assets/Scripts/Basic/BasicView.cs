using UnityEngine;
using System;
using System.Collections;
using UnityEngine.UI;
using DG.Tweening;
using NaughtyAttributes;

public abstract class BasicView : MonoBehaviour
{
    [Header("Basic YieldShow")]
    [SerializeField] bool IsShowYieldShow;
    [ShowIf(nameof(IsShowYieldShow))] [SerializeField] protected CanvasGroup MainCanvasGroup;

    [Header("Basic FadeIn")]
    [SerializeField] bool IsFadeIn;
    [ShowIf(nameof(IsFadeIn))] [SerializeField] protected float FadeInTime = 1;

    [Header("Basic CloseBtn")]
    [SerializeField] bool IsShowClose;
    [ShowIf(nameof(IsShowClose))] [SerializeField] protected Button BgBtn;
    [ShowIf(nameof(IsShowClose))] [SerializeField] protected Button CloseBtn;

    [Header("Basic PopUp")]
    [SerializeField] bool IsUsePopUp;
    [ShowIf(nameof(IsUsePopUp))] [SerializeField] protected RectTransform PopUpRect;
    [ShowIf(nameof(IsUsePopUp))] [SerializeField] protected float PopUpTime = 0.5f;

    protected Action CloseAction;

    protected virtual void OnDestroy()
    {
        StopAllCoroutines();
    }

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

        PopUpEffect();
    }

    /// <summary>
    /// 淡入顯示
    /// </summary>
    protected IEnumerator IFadeInShow()
    {
        if (MainCanvasGroup == null)
            yield break;

        MainCanvasGroup.alpha = 0;

        float currentTime = 0f;
        while (currentTime < FadeInTime)
        {
            currentTime += Time.deltaTime;
            float progress = currentTime / FadeInTime;
            MainCanvasGroup.alpha = Mathf.Lerp(0, 1, progress);

            yield return null;
        }

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
