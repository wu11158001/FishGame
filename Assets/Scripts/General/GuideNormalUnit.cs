using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GuideNormalUnit : MonoBehaviour
{
    [SerializeField] Image CoverImage;
    [SerializeField] TextMeshProUGUI MagnificationText;

    public void SetData(Sprite sprite, double magnification)
    {
        CoverImage.sprite = sprite;
        MagnificationText.text = $"{StringUtility.CurrencyFormat(magnification)}X";
    }
}
