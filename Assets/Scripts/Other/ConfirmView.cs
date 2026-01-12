using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class ConfirmView : BasicView
{
    [Header("ConfirmView")]
    [SerializeField] TextMeshProUGUI ContentText;
    [SerializeField] Button ConfirmBtn;
    [SerializeField] Button CancelBtn;

    Action ComfirmAction;
    Action CancelAction;

    protected override void Start()
    {
        base.Start();

        ConfirmBtn.onClick.AddListener(() => { ComfirmAction?.Invoke(); });
        CancelBtn.onClick.AddListener(() => { CancelAction?.Invoke(); });

        StartCoroutine(IYieldShow());
    }

    public void SetData(string contentKey, Action comfirmAction, Action cancelAction)
    {
        ComfirmAction = comfirmAction;
        CancelAction = cancelAction;

        MainCanvasGroup.alpha = 0;

        ContentText.text = LocalizationManagement.Instance.GetLocalizedString(contentKey);
    }
}
