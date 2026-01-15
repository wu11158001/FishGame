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

    CoinStoreUnitData CoinStoreUnitData;

    private void Start()
    {
        BuyBtn.onClick.AddListener(BuyCoin);
    }

    public void SetData(CoinStoreUnitData data)
    {
        if(data == null)
        {
            Debug.LogError("金幣商店單位獲取資料錯誤");
            return;
        }

        CoinStoreUnitData = data;

        CoverImage.sprite = data.CoverSprite;
        GetCoinText.text = StringUtility.CurrencyFormat(data.CoinStoreData.GetCoin);
        BuyBtnText.text = $"$ : {StringUtility.CurrencyFormat(data.CoinStoreData.Price)}";
    }

    /// <summary>
    /// 更新帳戶資料
    /// </summary>
    /// <param name="accountData"></param>
    public void UpdateAccountData(AccountData accountData)
    {
        CoinStoreUnitData.AccountData = accountData;
    }

    /// <summary>
    /// 購買金幣
    /// </summary>
    private void BuyCoin()
    {
        Canvas_Global.Instance.ShowLoading();

        double newCoin = CoinStoreUnitData.AccountData.Coins + CoinStoreUnitData.CoinStoreData.GetCoin;

        var updates = new Dictionary<string, object>
        {
            { "Coins", newCoin }
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

                if (!res.IsSuccess) Debug.LogError("更新Firestore帳戶金幣資料失敗");
                else
                {
                    // 顯示獲得物品
                    AddressableManagement.Instance.ShowGetItemView(
                        iconSprite: CoinStoreUnitData.CoverSprite,
                        value: CoinStoreUnitData.CoinStoreData.GetCoin);
                }
            });
        }
    }
}
