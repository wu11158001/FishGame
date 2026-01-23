using UnityEngine;
using TMPro;
using DG.Tweening;

public class SeaKing : MonoBehaviour
{
    [SerializeField] DragonFullOdds DragonFullOdds;

    /// <summary>
    /// 文字效果
    /// </summary>
    public void TextEffect()
    {
        DragonFullOdds.TextPunchEffect();
    }
}
