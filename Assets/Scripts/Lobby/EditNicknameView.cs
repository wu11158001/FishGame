using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class EditNicknameView : BasicView
{
    [Header("EditNicknameView")]
    [SerializeField] TMP_InputField EditNicknameIF;
    [SerializeField] Button SendBtn;

    protected override void Start()
    {
        base.Start();

        EditNicknameIF.onValueChanged.AddListener((value) => { CheckInput(value); });
        SendBtn.onClick.AddListener(UpdateNickname);
    }

    private void Update()
    {
        // Enter
        if (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            UpdateNickname();
        }
    }

    private void Initialize()
    {
        SendBtn.interactable = false;
    }

    public void SetData(Action closeAction )
    {
        CloseAction = closeAction;

        MainCanvasGroup.alpha = 0;

        Initialize();

        StartCoroutine(IYieldShow());
    }

    /// <summary>
    /// 檢查輸入暱稱
    /// </summary>
    private bool CheckInput(string value)
    {
        if (value.Contains(" "))
        {
            // 去除所有空格並重新賦值
            EditNicknameIF.text = value.Replace(" ", "");
        }

        bool isCompliance = EditNicknameIF.text.Length >= 4 && EditNicknameIF.text.Length <= 12;

        SendBtn.interactable = isCompliance;

        return isCompliance;
    }

    /// <summary>
    /// 更新帳戶暱稱
    /// </summary>
    private void UpdateNickname()
    {
        if (FirestoreManagement.Instance != null && CheckInput(EditNicknameIF.text))
        {
            if (Canvas_Global.Instance)
                Canvas_Global.Instance.ShowLoading();

            // 更新帳戶頭像與頭相框
            var updates = new Dictionary<string, object>
            {
                { "Nickname", EditNicknameIF.text }
            };

            FirestoreManagement.Instance.UpdateDataToFirestore(
                path: FirestoreCollectionNameEnum.AccountData,
                docId: FirestoreDataManagement.Instance.CurrLoginInfo.Account,
                updates: updates,
                callback: (res) =>
                {
                    if (!res.IsSuccess) Debug.LogError("更新Firestore帳戶暱稱資料失敗");

                    Canvas_Global.Instance.CloseLoading();
                    CloseAction?.Invoke();
                });
        }
    }
}
