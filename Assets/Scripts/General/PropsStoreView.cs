using UnityEngine;
using System;
using System.Collections.Generic;

public class PropsStoreView : BasicView
{

    [Header("PropsStoreView")]
    [SerializeField] PropsStoreUnit PropsStoreUnit;
    [SerializeField] RectTransform ContentRect;

    List<PropsStoreUnit> PropsStoreUnitDatas = new();

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;

        MainCanvasGroup.alpha = 0;

        CreatePropsStoreUnit();
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
    /// 創建道具商品
    /// </summary>
    private void CreatePropsStoreUnit()
    {
        for (int i = 0; i < PropsStoreUnitDatas.Count; i++)
        {
            Destroy(PropsStoreUnitDatas[i].gameObject);
        }
        PropsStoreUnitDatas.Clear();

        int index = 0;
        PropsStoreUnit.gameObject.SetActive(false);
        foreach (PropsEnum propsType in Enum.GetValues(typeof(PropsEnum)))
        {
            if (propsType == PropsEnum.None)
                continue;

            GameObject obj = Instantiate(PropsStoreUnit.gameObject, ContentRect);
            obj.SetActive(true);
            PropsStoreUnit propsStoreUnit = obj.GetComponent<PropsStoreUnit>();

            if (propsStoreUnit != null)
            {
                Sprite coverSprite = TextureManagement.Instance.GetPropsTexture(propsType);
                propsStoreUnit.SetData(coverSprite: coverSprite, propsType: propsType);
                PropsStoreUnitDatas.Add(propsStoreUnit);
            }
            else
            {
                Debug.LogError("創建道具商品錯誤");
            }

            index++;
        }
    }
}