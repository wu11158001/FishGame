using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class RegisterRewardView : BasicView
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

    public void SetData(double reward)
    {
        Reward = reward;

        RewardValueText.text = $"X{StringUtility.CurrencyFormat(reward)}";
    }

    /// <summary>
    /// 接收獎勵
    /// </summary>
    private void ReciveReward()
    {
        // 顯示獲得獎勵
        AddressableManagement.Instance.ShowGetItemView(
            iconSprite: CoinSprite,
            value: Reward);

        // 更新註冊時間
        long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var updates = new Dictionary<string, object>
        {
            { "RegisterTime", currentTimestamp }
        };

        if (FirestoreManagement.Instance != null)
        {
            FirestoreManagement.Instance.UpdateDataToFirestore(
            path: FirestoreCollectionNameEnum.AccountData,
            docId: FirestoreManagement.Instance.CurrLoginInfo.Account,
            updates: updates,
            callback: (res) =>
            {
                if (!res.IsSuccess) Debug.LogError("更新Firestore帳戶更新註冊時間失敗");
            });
        }
    }
}
