using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class LoginRewardView : BasicView
{
    [Header("LoginRewardView")]
    [SerializeField] Button ReciveBtn;
    [SerializeField] Sprite CoinSprite;
    [SerializeField] TextMeshProUGUI RewardValueText;
    [SerializeField] RectTransform CoinIcon;

    LobbyView LobbyView;

    protected override void OnDestroy()
    {
        base.OnDestroy();

        CoinIcon.DOKill();
    }

    public override void Close()
    {
        if (LobbyView != null)
            LobbyView.EffectObjectShowControl(true);

        base.Close();
    }

    protected override void Start()
    {
        base.Start();

        ReciveBtn.onClick.AddListener(ReciveReward);

        CoinIcon.DOKill();
        CoinIcon.DOJumpAnchorPos(Vector2.zero, 15, 2, 1.5f)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
    }

    public void SetData(LobbyView lobbyView, Action closeAction)
    {
        LobbyView = lobbyView;
        CloseAction = closeAction;

        MainCanvasGroup.alpha = 0;
        lobbyView.EffectObjectShowControl(false);

        if (FirestoreDataManagement.Instance != null)
        {
            RewardValueText.text = $"X{StringUtility.CurrencyFormat(FirestoreDataManagement.Instance.LoginAndRegisterData.LoginReward)}";
            StartCoroutine(IYieldShow());
        }
    }

    /// <summary>
    /// 接收獎勵
    /// </summary>
    private void ReciveReward()
    {
        if (FirestoreManagement.Instance == null || FirestoreDataManagement.Instance == null)
        {
            Debug.LogError("接收登入獎勵錯誤!");
            return;
        }

        Canvas_Global.Instance.ShowLoading();

        // 顯示獲得獎勵
        AddressableManagement.Instance.ShowGetItemView(
            iconSprite: CoinSprite,
            value: FirestoreDataManagement.Instance.LoginAndRegisterData.LoginReward);

        // 更新最後登入時間 & 帳戶金幣
        DateTime taiwanTime = DateTime.UtcNow.AddHours(8);
        string currentTimestamp = taiwanTime.ToString("yyyy-MM-dd HH:mm:ss");
        double currAccountCoin = FirestoreDataManagement.Instance.CurrAccountData.Coins;

        var updates = new Dictionary<string, object>
        {
            { "Coins", currAccountCoin + FirestoreDataManagement.Instance.LoginAndRegisterData.LoginReward},
            { "LastLoginTime", currentTimestamp },
        };

        FirestoreManagement.Instance.UpdateDataToFirestore(
            path: FirestoreCollectionNameEnum.AccountData,
            docId: FirestoreDataManagement.Instance.CurrLoginInfo.Account,
            updates: updates,
            callback: (res) =>
            {
                Canvas_Global.Instance.CloseLoading();

                if (!res.IsSuccess) Debug.LogError("更新Firestore帳戶最後登入時間失敗");
                Close();
            });
    }
}
