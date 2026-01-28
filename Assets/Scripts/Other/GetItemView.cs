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
        UIUtility.SetMaxUISize(targetRt: ItemImage.rectTransform, maxSize: 256f);

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
