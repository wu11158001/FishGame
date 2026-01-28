using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class GetItemView : BasicView
{
    [Header("Main")]
    [SerializeField] Image ItemImage;
    [SerializeField] TextMeshProUGUI ValueText;
    [SerializeField] float ShowTime = 3;    

    protected override void Start()
    {
        base.Start();

        StartCoroutine(IShowItem());
    }

    public void SetData(Sprite iconSprite, double value, Action closeAction)
    {
        CloseAction = closeAction;

        ItemImage.sprite = iconSprite;
        ItemImage.SetNativeSize();

        RectTransform rt = ItemImage.rectTransform;
        // 最大圖片大小
        float maxSize = 256f;

        // 取得圖片目前的寬高
        float width = rt.sizeDelta.x;
        float height = rt.sizeDelta.y;

        // 如果寬或高超過了限制
        if (width > maxSize || height > maxSize)
        {
            // 計算縮放比率，取其最長的一邊作為基準
            float ratio = maxSize / Mathf.Max(width, height);
            rt.sizeDelta = new Vector2(width * ratio, height * ratio);
        }

        ValueText.text = $"X {StringUtility.CurrencyFormat(value)}";        
    }

    /// <summary>
    /// 物品顯示效果
    /// </summary>
    private IEnumerator IShowItem()
    {
        yield return IFadeInShow();
        yield return new WaitForSeconds(ShowTime);

        Close();
    }
}
