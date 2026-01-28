using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CoinStoreUnit : MonoBehaviour
{
    [SerializeField] Image CoverImage;
    [SerializeField] TextMeshProUGUI GetCoinText;
    [SerializeField] Button BuyBtn;
    [SerializeField] TextMeshProUGUI BuyBtnText;

    CoinStoreData CoinStoreData;
    Sprite CoverSprite;

    private void Start()
    {
        // 購買按鈕
        BuyBtn.onClick.AddListener(BuyCoin);
    }

    public void SetData(Sprite coverSprite, ShopCoinEnum coinType)
    {
        CoverSprite = coverSprite;
        CoinStoreData = FirestoreDataManagement.Instance?.GetCoinStoreData(coinType);

        if(CoinStoreData == null)
        {
            Debug.LogError($"獲取金幣資料錯誤: {coinType}");
            Destroy(gameObject);
        }

        CoverImage.sprite = coverSprite;
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
