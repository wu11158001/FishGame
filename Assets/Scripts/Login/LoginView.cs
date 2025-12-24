using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Localization.Components;

public class LoginView : BasicView
{
    [Header("SwitchArea")]
    [SerializeField] Toggle LoginTog;
    [SerializeField] Toggle RegisterTog;

    [Header("LanguageArea")]
    [SerializeField] Toggle ChineseTog;
    [SerializeField] Toggle EnglishTog;

    [Header("SwitchArea")]
    [SerializeField] LocalizeStringEvent TitleText;
    [SerializeField] GameObject LoginArea;
    [SerializeField] GameObject RegisterArea;

    /// <summary>
    /// 切換面板類型
    /// </summary>
    private enum PanelType
    {
        Login,      // 登入
        Register    // 註冊
    }

    private void Start()
    {
        LoginTog.onValueChanged.AddListener((value) =>
        {
            if(value == true)
                SwitchPanel(PanelType.Login);
        });

        RegisterTog.onValueChanged.AddListener((value) =>
        {
            if (value == true)
                SwitchPanel(PanelType.Register);
        });

        ChineseTog.onValueChanged.AddListener((value) =>
        {
            if (value == true)
                LocalizationManagement.Instance.ChangeLanguage(Language.zh_TW);
        });

        EnglishTog.onValueChanged.AddListener((value) =>
        {
            if (value == true)
                LocalizationManagement.Instance.ChangeLanguage(Language.en);
        });
    }

    private void Initialize()
    {
        SwitchPanel(PanelType.Login);

        LoginTog.isOn = true;
        ChineseTog.isOn = true;
    }

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;

        Initialize();
    }

    /// <summary>
    /// 切換面板
    /// </summary>
    /// <param name="panelType"></param>
    private void SwitchPanel(PanelType panelType)
    {
        LoginArea.SetActive(panelType == PanelType.Login);
        RegisterArea.SetActive(panelType == PanelType.Register);

        string newKey =
            panelType == PanelType.Login ?
            "Log In" :
            "Register";

        LocalizationManagement.Instance.UpdateKey(
            localizeEvent: TitleText,
            newKey: newKey);        
    }
}
