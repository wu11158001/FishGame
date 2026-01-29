using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SevenDayUnit : MonoBehaviour
{
    [SerializeField] Image RewardImage;
    [SerializeField] TextMeshProUGUI RewardValueText;
    [SerializeField] Button SignInBtn;
    [SerializeField] TextMeshProUGUI SignInBtnText;
    [SerializeField] Animator SignInDayAni;
    [SerializeField] GameObject ExpiredObj;
    [SerializeField] Image DayNumberImage;

    Action SignInAction;

    private void Start()
    {
        SignInBtn.onClick.AddListener(() => { SignInAction?.Invoke(); });
    }

    public void SetData(SevenDayUnitData sevenDayUnitData)
    {
        SignInAction = sevenDayUnitData.SignInAction;
        RewardValueText.text = StringUtility.CurrencyFormat(sevenDayUnitData.RewardValue);
        SignInDayAni.enabled = sevenDayUnitData.IsSignInDay;
        SignInBtn.interactable = sevenDayUnitData.IsSignInDay && !sevenDayUnitData.IsReceived;
        ExpiredObj.SetActive(sevenDayUnitData.IsExpired);
        DayNumberImage.sprite = sevenDayUnitData.NumberSprite;
        RewardImage.sprite = sevenDayUnitData.RewardSprite;
        RewardImage.SetNativeSize();
        UIUtility.SetMaxUISize(targetRt: RewardImage.rectTransform, maxSize: 83f);

        // 簽到
        string btnStr = LocalizationManagement.Instance.GetLocalizedString("Sign in");
        if(sevenDayUnitData.IsExpired)
        {
            // 已過期
            btnStr = LocalizationManagement.Instance.GetLocalizedString("Expired");
        }
        else if(sevenDayUnitData.IsReceived)
        {
            // 已領取
            btnStr = LocalizationManagement.Instance.GetLocalizedString("Received");
        }

        SignInBtnText.text = btnStr;
    }
}
