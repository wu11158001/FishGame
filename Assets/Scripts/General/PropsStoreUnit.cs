using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PropsStoreUnit : MonoBehaviour
{
    [SerializeField] Image CoverImage;
    [SerializeField] TMP_InputField CountIF;
    [SerializeField] Button ReduceBtn;
    [SerializeField] Button AddBtn;
    [SerializeField] Button BuyBtn;
    [SerializeField] TextMeshProUGUI BuyBtnText;

    PropsStoreData PropsStoreData;
    Sprite CoverSprite;
    int BuyCount = 1;

    private void Start()
    {
        // 購買按鈕
        BuyBtn.onClick.AddListener(BuyCoin);

        // 減少購買數量按鈕
        ReduceBtn.onClick.AddListener(() =>
        {
            BuyCount = Mathf.Max(1, --BuyCount);
            BuyCountChange();
        });

        // 數量輸入框
        CountIF.onValueChanged.AddListener((value) =>
        {
            if (string.IsNullOrEmpty(value))
            {
                BuyCount = 1;
                BuyCountChange();
            }
            else
            {
                BuyCount = Mathf.Max(1, int.Parse(value));
                BuyCount = Mathf.Min(LocalData.MaxPropsBuyCount, int.Parse(value));
                BuyCountChange();
            }
        });

        // 增加購買數量按鈕
        AddBtn.onClick.AddListener(() =>
        {
            BuyCount = Mathf.Min(99, ++BuyCount);
            BuyCountChange();
        });
    }

    public void SetData(Sprite coverSprite, PropsEnum propsType)
    {
        CoverSprite = coverSprite;
        PropsStoreData = FirestoreDataManagement.Instance?.GetPropsStoreData(propsType);

        if (PropsStoreData == null)
        {
            Debug.LogError($"獲取道具資料錯誤: {propsType}");
            Destroy(gameObject);
        }

        CoverImage.sprite = coverSprite;
        CoverImage.sprite = coverSprite;
        BuyCount = 1;

        BuyCountChange();
    }

    /// <summary>
    /// 購買數量變更
    /// </summary>
    private void BuyCountChange()
    {
        CountIF.text = StringUtility.CurrencyFormat(BuyCount);

        double totalPrice = PropsStoreData.UnitPrice * BuyCount;
        BuyBtnText.text = $"$ : {StringUtility.CurrencyFormat(totalPrice)}";
    }

    /// <summary>
    /// 購買道具
    /// </summary>
    private void BuyCoin()
    {
        Canvas_Global.Instance.ShowLoading();

        double totalPrice = PropsStoreData.UnitPrice * BuyCount;
        double newCoin = FirestoreDataManagement.Instance.CurrAccountData.Coins - totalPrice;
        PropsEnum propsType = PropsStoreData.PropsType;

        int newCount = 0;
        switch (propsType)
        {
            // 冰凍道具
            case PropsEnum.Freeze:
                newCount = FirestoreDataManagement.Instance.CurrAccountData.FreezeProps + BuyCount;
                break;
        }

        if (propsType == PropsEnum.None)
        {
            Debug.LogError($"道具商店單位獲取資料錯誤!");
            return;
        }

        if (newCoin < 0)
        {
            // 金幣不足!
            AddressableManagement.Instance.ShowToast("Insufficient Coin");
            _ = AddressableManagement.Instance.OpenCoinStoreView();
            return;
        }

        var updates = new Dictionary<string, object>
        {
            { "Coins", newCoin },
            { $"{propsType}Props", newCount}
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

                if (!res.IsSuccess) Debug.LogError("更新Firestore帳戶金幣與道具資料失敗");
                else
                {
                    // 顯示獲得物品
                    AddressableManagement.Instance.ShowGetItemView(
                        iconSprite: CoverSprite,
                        value: BuyCount);
                }
            });
        }
    }
}
