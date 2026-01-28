using UnityEngine;
using System;
using System.Collections.Generic;

public class CoinStoreView : BasicView
{
    [Header("CoinStoreView")]
    [SerializeField] CoinStoreUnit CoinStoreUnit;
    [SerializeField] RectTransform ContentRect;

    List<CoinStoreUnit> CoinStoreUnitDatas = new();

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;

        MainCanvasGroup.alpha = 0;

        CreateCoinStoreUnit();
        StartCoroutine(IYieldShow());
    }

    /// <summary>
    /// 控制顯示
    /// </summary>
    public void CanvasGroupShow(bool isShow)
    {
        MainCanvasGroup.alpha = isShow ? 1 : 0;
    }

    /// <summary>
    /// 創建金幣商品
    /// </summary>
    private void CreateCoinStoreUnit()
    {
        for (int i = 0; i < CoinStoreUnitDatas.Count; i++)
        {
            Destroy(CoinStoreUnitDatas[i].gameObject);
        }
        CoinStoreUnitDatas.Clear();

        int index = 0;
        CoinStoreUnit.gameObject.SetActive(false);
        foreach (ShopCoinEnum coinType in Enum.GetValues(typeof(ShopCoinEnum)))
        {
            if (coinType == ShopCoinEnum.None)
                continue;

            GameObject obj = Instantiate(CoinStoreUnit.gameObject, ContentRect);
            obj.SetActive(true);
            CoinStoreUnit coinStoreUnit = obj.GetComponent<CoinStoreUnit>();

            if(coinStoreUnit != null)
            {
                Sprite coverSprite = TextureManagement.Instance.GetCoinTexture(coinType);
                coinStoreUnit.SetData(coverSprite: coverSprite, coinType: coinType);
                CoinStoreUnitDatas.Add(coinStoreUnit);
            }
            else
            {
                Debug.LogError("創建金幣商品錯誤");
            }

            index++;
        }
    }
}