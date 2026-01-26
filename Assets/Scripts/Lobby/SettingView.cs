using UnityEngine;
using UnityEngine.UI;
using System;

public class SettingView : BasicView
{
    [Header("SettingView")]
    [SerializeField] Button SignOutBtn;

    [Header("LanguageArea")]
    [SerializeField] Toggle ChineseTog;
    [SerializeField] Toggle EnglishTog;

    protected override void Start()
    {
        base.Start();

        // 登出按鈕
        SignOutBtn.onClick.AddListener(SignOut);

        // 中文語言
        ChineseTog.onValueChanged.AddListener((value) =>
        {
            if (value == true)
                LocalizationManagement.Instance.ChangeLanguage(Language.zh_TW);
        });

        // 英文語言
        EnglishTog.onValueChanged.AddListener((value) =>
        {
            if (value == true)
                LocalizationManagement.Instance.ChangeLanguage(Language.en);
        });
    }

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;

        MainCanvasGroup.alpha = 0;

        int localLanguage = PlayerPrefs.GetInt(PlayerPrefsManagement.LANGUAGE);
        if(localLanguage <= 0 || localLanguage > Enum.GetNames(typeof(Language)).Length)
        {
            ChineseTog.isOn = true;
        }
        else
        {
            EnglishTog.isOn = true;
        }

        StartCoroutine(IYieldShow());
    }

    /// <summary>
    /// 登出
    /// </summary>
    private void SignOut()
    {
        Canvas_Global.Instance.ShowLoading();
        FirestoreDataManagement.Instance.StopHeartbeat();

        SceneManagement.Instance.LoadScene(
            sceneEnum: SceneEnum.Login,
            callback: async () =>
            {
                await AddressableManagement.Instance.OpenLoginView(isLogout: true);
            });

        CloseAction?.Invoke();
    }
}
