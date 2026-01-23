using UnityEngine;
using System.Collections;
using TMPro;
using DG.Tweening;
using System;

/// <summary>
/// 金龍獲得場上魚群倍率效果物件
/// </summary>
public class DragonFullOdds : MonoBehaviour
{
    [SerializeField] Animator Animator;
    [SerializeField] SpriteRenderer Mask;
    [SerializeField] TextMeshPro OddsText;
    [SerializeField] GameObject SeaKingObj;

    [Header("GetRewardArea")]
    [SerializeField] GameObject GetRewardAreaObj;
    [SerializeField] TextMeshPro TotalRewardText;
    [SerializeField] EruptionAndRecycleEffect EruptionCoin;

    int CurrOdds = 0;
    int TargetOdds = 0;
    double TotalReward = 0;
    Vector3 TargetRecycle;

    Action FinishCallback;

    private void OnDestroy()
    {
        StopAllCoroutines();
        OddsText.rectTransform.DOKill();
        TotalRewardText.rectTransform.DOKill();
    }

    private void Initialize()
    {
        OddsText.rectTransform.DOKill();
        Mask.color = new(0, 0, 0, 1);
        OddsText.text = $"X{0}";

        SeaKingObj.SetActive(true);
        OddsText.gameObject.SetActive(true);
        GetRewardAreaObj.SetActive(false);
    }

    public void SetData(Vector3 targetRecycle, int targerOdds, double totalReward, Action finishCallback)
    {
        TargetRecycle = targetRecycle;
        TargetOdds = targerOdds;
        TotalReward = totalReward;
        FinishCallback = finishCallback;

        Initialize();
        StartCoroutine(IEffect());
    }

    /// <summary>
    /// 效果總控
    /// </summary>
    /// <returns></returns>
    private IEnumerator IEffect()
    {
        yield return IMaskFadeOut();

        Animator.SetTrigger("RightHand");

        yield return new WaitForSeconds(1.5f);

        Animator.SetTrigger("LeftHand");

        yield return new WaitForSeconds(1.5f);

        Animator.SetTrigger("TwoHand");

        yield return new WaitForSeconds(1.5f);

        ShowReward();
        EruptionCoin.SetData(targetRecycle: TargetRecycle);

        yield return new WaitForSeconds(3f);

        FinishCallback?.Invoke();

        Destroy(gameObject);
    }

    /// <summary>
    /// 顯示最終獎勵
    /// </summary>
    private void ShowReward()
    {
        SeaKingObj.SetActive(false);
        OddsText.gameObject.SetActive(false);
        GetRewardAreaObj.SetActive(true);

        TotalRewardText.text = StringUtility.CurrencyFormat(TotalReward);

        // 文字震動效果
        TotalRewardText.rectTransform.DOKill();
        TotalRewardText.rectTransform.localScale = Vector3.one;
        TotalRewardText.rectTransform.DOPunchScale(new Vector3(0.5f, 0.5f, 0.5f), 0.5f, 10, 1f);
    }

    /// <summary>
    /// 遮罩淡出效果
    /// </summary>
    /// <returns></returns>
    private IEnumerator IMaskFadeOut()
    {
        float duration = 2f;
        float currentTime = 0f;
        Color startColor = Mask.color;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, currentTime / duration);
            Mask.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        // 確保最後完全透明
        Mask.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
    }

    /// <summary>
    /// 文字震動效果
    /// </summary>
    public void TextPunchEffect()
    {
        // 倍率上升
        if (CurrOdds < TargetOdds)
            CurrOdds++;

        OddsText.text = $"X{CurrOdds}";

        // 文字震動效果
        OddsText.rectTransform.DOKill();
        OddsText.rectTransform.localScale = Vector3.one;
        OddsText.rectTransform.DOPunchScale(new Vector3(0.5f, 0.5f, 0.5f), 0.5f, 10, 1f);
    }
}
