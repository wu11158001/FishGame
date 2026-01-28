using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class CoinStoreUnit : MonoBehaviour
{
    [Header("CoinStoreUnit")]
    [SerializeField] float MaxCoverSize = 256f;
    [SerializeField] Button MainBtn;
    [SerializeField] RectTransform MainRect;
    [SerializeField] Image CoverImage;
    [SerializeField] TextMeshProUGUI GetCoinText;
    [SerializeField] Button BuyBtn;
    [SerializeField] TextMeshProUGUI BuyBtnText;

    CoinStoreData CoinStoreData;
    Sprite CoverSprite;
    Action<RectTransform> SelectAction;

    private void Start()
    {
        // 滑動條移動至顯示位置
        MainBtn.onClick.AddListener(() => { SelectAction?.Invoke(MainRect); });

        // 購買按鈕
        BuyBtn.onClick.AddListener(BuyCoin);
    }

    public void SetData(Sprite coverSprite, ShopCoinEnum coinType, Action<RectTransform> selectAction)
    {
        SelectAction = selectAction;
        CoverSprite = coverSprite;
        CoinStoreData = FirestoreDataManagement.Instance?.GetCoinStoreData(coinType);

        if(CoinStoreData == null)
        {
            Debug.LogError($"獲取金幣資料錯誤: {coinType}");
            Destroy(gameObject);
        }

        CoverImage.sprite = coverSprite;
        CoverImage.SetNativeSize();
        UIUtility.SetMaxUISize(targetRt: CoverImage.rectTransform, maxSize: MaxCoverSize);

        GetCoinText.text = StringUtility.CurrencyFormat(CoinStoreData.GetCoin);
        BuyBtnText.text = $"$ : {StringUtility.CurrencyFormat(CoinStoreData.Price)}";
    }

    /// <summary>
    /// 購買金幣
    /// </summary>
    private void BuyCoin()
    {
        Canvas_Global.Instance.ShowLoading();

        double newCoin = FirestoreDataManagement.Instance.CurrAccountData.Coins + CoinStoreData.GetCoin;

        var updates = new Dictionary<string, object>
        {
            { "Coins", newCoin }
        };

        if (FirestoreManagement.Instance != null && FirestoreDataManagement.Instance != null)
        {
            FirestoreManagement.Instance.UpdateDataToFirestore(
            path: FirestoreCollectionNameEnum.AccountData,
            docId: FirestoreDataManagement.Instance.CurrLoginInfo.Account,
            updates: updates,
            callback: (res) =>
            {
                Canvas_Global.Instance.CloseLoading();

                if (!res.IsSuccess) Debug.LogError("更新Firestore帳戶金幣資料失敗");
                else
                {
                    // 顯示獲得物品
                    AddressableManagement.Instance.ShowGetItemView(
                        iconSprite: CoverSprite,
                        value: CoinStoreData.GetCoin);
                }
            });
        }
    }
}
