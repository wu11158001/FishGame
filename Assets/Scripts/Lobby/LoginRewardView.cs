using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Newtonsoft.Json;

public class LoginRewardView : BasicView
{
    [Header("LoginRewardView")]
    [SerializeField] Button ReciveBtn;
    [SerializeField] Sprite CoinSprite;
    [SerializeField] TextMeshProUGUI RewardValueText;
    [SerializeField] RectTransform CoinIcon;

    double Reward;

    protected override void OnDestroy()
    {
        base.OnDestroy();

        CoinIcon.DOKill();
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

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;
        MainCanvasGroup.alpha = 0;

        // 獲取登入獎勵資料
        if (FirestoreManagement.Instance != null)
        {
            FirestoreManagement.Instance.GetDataFromFirestore(
                path: FirestoreCollectionNameEnum.ActivityData,
                docId: FirestoreActivityDataFileNameEnum.LoginAndRegister.ToString(),
                callback: GetLoginRewardCallback);
        }
    }

    /// <summary>
    /// 獲取登入獎勵資料Callback
    /// </summary>
    private void GetLoginRewardCallback(FirestoreResponse response)
    {
        if (response.IsSuccess)
        {
            try
            {
                LoginAndRegisterData data = JsonConvert.DeserializeObject<LoginAndRegisterData>(response.JsonData);
                if (data != null)
                {
                    Reward = data.LoginReward;
                    RewardValueText.text = $"X{StringUtility.CurrencyFormat(data.LoginReward)}";
                    StartCoroutine(IYieldShow());
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"獲取獲取登入獎勵資料錯誤: {e}");
                Close();
            }
        }
    }

    /// <summary>
    /// 接收獎勵
    /// </summary>
    private void ReciveReward()
    {
        Canvas_Global.Instance.ShowLoading();

        // 顯示獲得獎勵
        AddressableManagement.Instance.ShowGetItemView(
            iconSprite: CoinSprite,
            value: Reward);

        // 更新最後登入時間 & 帳戶金幣
        DateTime taiwanTime = DateTime.UtcNow.AddHours(8);
        string currentTimestamp = taiwanTime.ToString("yyyy-MM-dd HH:mm:ss");
        double currAccountCoin = FirestoreManagement.Instance.CurrAccountData.Coins;
        var updates = new Dictionary<string, object>
        {
            { "Coins", currAccountCoin + Reward},
            { "LastLoginTime", currentTimestamp },
        };

        if (FirestoreManagement.Instance != null)
        {
            FirestoreManagement.Instance.UpdateDataToFirestore(
            path: FirestoreCollectionNameEnum.AccountData,
            docId: FirestoreManagement.Instance.CurrLoginInfo.Account,
            updates: updates,
            callback: (res) =>
            {
                Canvas_Global.Instance.CloseLoading();

                if (!res.IsSuccess) Debug.LogError("更新Firestore帳戶最後登入時間失敗");
                Close();
            });
        }
    }
}
