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
    [SerializeField] float MaxIconSize = 200f;
    [SerializeField] float ShowTime = 2;
    [SerializeField] float TextEffectSpeed = 0.05f;

    public void SetData(Sprite iconSprite, double value, Action closeAction)
    {
        CloseAction = closeAction;

        ItemImage.sprite = iconSprite;
        ItemImage.SetNativeSize();
        UIUtility.SetMaxUISize(targetRt: ItemImage.rectTransform, maxSize: MaxIconSize);

        ValueText.gameObject.SetActive(value > 1);
        StartCoroutine(ITextEffect($"X {StringUtility.CurrencyFormat(value)}"));
    }

    /// <summary>
    /// 砲台能力文字效果
    /// </summary>
    private IEnumerator ITextEffect(string str)
    {
        ValueText.text = str;
        ValueText.maxVisibleCharacters = 0;

        yield return IFadeInShow();

        int totalCharacters = str.Length;

        for (int i = 0; i <= totalCharacters; i++)
        {
            ValueText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(TextEffectSpeed);
        }

        yield return new WaitForSeconds(ShowTime);
        Close();
    }
}
