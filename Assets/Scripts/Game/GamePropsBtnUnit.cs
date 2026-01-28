using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 遊戲道具按鈕
/// </summary>
public class GamePropsBtnUnit : MonoBehaviour
{
    [SerializeField] Button MainBtn;
    [SerializeField] Image CoverImage;
    [SerializeField] TextMeshProUGUI CountText;

    PropsEnum PropsType;
    Action ClickAction;

    private void OnDestroy()
    {
        if (FirestoreDataManagement.Instance != null)
            FirestoreDataManagement.Instance.AccountDataChangeDelegate -= AccountDataChange;
    }

    private void Start()
    {
        MainBtn.onClick.AddListener(() => { ClickAction?.Invoke(); });

        if (FirestoreDataManagement.Instance != null)
            FirestoreDataManagement.Instance.AccountDataChangeDelegate += AccountDataChange;
    }

    public void SetData(PropsEnum propsType, Action clickAction)
    {
        ClickAction = clickAction;
        PropsType = propsType;
        CoverImage.sprite = TextureManagement.Instance.GetPropsTexture(propsType);

        AccountDataChange(FirestoreDataManagement.Instance.CurrAccountData);
    }

    /// <summary>
    /// 帳戶資料變更
    /// </summary>
    private void AccountDataChange(AccountData accountData)
    {
        if (accountData == null)
            return;

        int count = 0;

        switch (PropsType)
        {
            case PropsEnum.Freeze:
                count = accountData.FreezeProps;
                break;
        }

        CountText.text = $"X{StringUtility.CurrencyFormat(count)}";
    }
}
