using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using System;

public class PropsStoreUnit : MonoBehaviour
{
    [Header("PropsStoreUnit")]
    [SerializeField] float MaxCoverSize = 256f;
    [SerializeField] Button MainBtn;
    [SerializeField] RectTransform MainRect;
    [SerializeField] Image CoverImage;
    [SerializeField] TMP_InputField CountIF;
    [SerializeField] Button ReduceBtn;
    [SerializeField] Button AddBtn;
    [SerializeField] Button BuyBtn;
    [SerializeField] TextMeshProUGUI BuyBtnText;

    [Header("Describe Area")]
    [SerializeReference] EventSystemsHandler EventSystemsHandler;
    [SerializeField] RectTransform DescribeRect;
    [SerializeField] TextMeshProUGUI DescribeText;

    PropsStoreData PropsStoreData;
    Sprite CoverSprite;
    int BuyCount = 1;

    Action<RectTransform> SelectAction;

    private void OnDestroy()
    {
        DescribeRect.DOKill();
        EventSystemsHandler.PointerEnterHandleDelegate -= ShowDescribe;
    }

    private void Start()
    {
        // 滑動條移動至顯示位置
        MainBtn.onClick.AddListener(() => { SelectAction?.Invoke(MainRect); });

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

        // 描述區域上下移動
        DescribeRect.DOKill();
        DescribeRect.DOLocalMoveY(DescribeRect.anchoredPosition.y + 10, 1.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        EventSystemsHandler.PointerEnterHandleDelegate += ShowDescribe;
    }

    public void SetData(Sprite coverSprite, PropsEnum propsType, Action<RectTransform> selectAction)
    {
        SelectAction = selectAction;
        CoverSprite = coverSprite;
        PropsStoreData = FirestoreDataManagement.Instance?.GetPropsStoreData(propsType);

        if (PropsStoreData == null)
        {
            Debug.LogError($"獲取道具資料錯誤: {propsType}");
            Destroy(gameObject);
        }

        CoverImage.sprite = coverSprite;
        CoverImage.SetNativeSize();
        UIUtility.SetMaxUISize(targetRt: CoverImage.rectTransform, maxSize: MaxCoverSize);

        int ownCount = 0;
        BuyCount = 1;

        // 設置描述內容
        string tableName = LocalizationManagement.Instance.TableName;
        LocalizedString DescribeLocalized = new();
        switch (propsType)
        {
            // 冰凍描述
            case PropsEnum.Freeze:
                // 冰凍全屏魚 {0}秒。
                DescribeLocalized.SetReference(tableName, "Freeze Message");
                DescribeLocalized.Arguments = new object[] { LocalData.FreezeTime };

                ownCount = FirestoreDataManagement.Instance.CurrAccountData.FreezeProps;
                break;
        }

        string ownStr = LocalizationManagement.Instance.GetLocalizedString("Own");
        

        // 描述 \n 擁有: X
        DescribeText.text = $"{DescribeLocalized.GetLocalizedString()}\n{ownStr} : {ownCount}";
        DescribeRect.gameObject.SetActive(false);

        Canvas.ForceUpdateCanvases();
        BuyCountChange();
    }

    /// <summary>
    /// 顯示描述區域
    /// </summary>
    private void ShowDescribe(PointerEventData eventData, bool isEnter)
    {
        DescribeRect.gameObject.SetActive(isEnter);
    }

    /// <summary>
    /// 購買數量變更
    /// </summary>
    private void BuyCountChange()
    {
        CountIF.text = StringUtility.CurrencyFormat(BuyCount);

        double totalPrice = PropsStoreData.UnitPrice * BuyCount;
        BuyBtnText.text = StringUtility.CurrencyFormat(totalPrice);
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
