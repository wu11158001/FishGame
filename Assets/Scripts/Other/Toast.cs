using UnityEngine;
using UnityEngine.Localization.Components;
using System;
using DG.Tweening;

public class Toast : BasicView
{
    [SerializeField] RectTransform PanelRect;
    [SerializeField] LocalizeStringEvent MessageText;

    float MoveTime = 0.15f;
    Vector2 StopPos = new(0, -120);
    float StopTime = 3f;

    private void OnDestroy()
    {
        PanelRect.DOKill();
    }

    void Start()
    {
        RunMoveEffect();
    }

    public void SetData(string messageKey, Action closeAction)
    {
        CloseAction = closeAction;
        LocalizationManagement.Instance.UpdateKey(MessageText, messageKey);
    }

    /// <summary>
    /// 移動效果
    /// </summary>
    private void RunMoveEffect()
    {
        Vector2 startPos = new(0, PanelRect.sizeDelta.y * 2);
        PanelRect.anchoredPosition = startPos;

        Sequence toastSequence = DOTween.Sequence();

        toastSequence            
            .Append(PanelRect.DOAnchorPos(StopPos, MoveTime).SetEase(Ease.OutQuad))// 滑進            
            .AppendInterval(StopTime)// 停頓            
            .Append(PanelRect.DOAnchorPos(startPos, MoveTime).SetEase(Ease.InQuad))// 滑出
            .OnComplete(() => { Close(); });
    }
}