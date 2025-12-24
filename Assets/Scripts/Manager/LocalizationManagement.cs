using UnityEngine;
using UnityEngine.Localization.Settings;
using System.Collections;
using UnityEngine.Localization.Components;

public class LocalizationManagement : SingletonMonoBehaviour<LocalizationManagement>
{    
    private readonly string TableName = "LanguageTable";

    bool IsChanging;

    /// <summary>
    /// 更換語言
    /// </summary>
    public void ChangeLanguage(Language language)
    {
        if (IsChanging)
            return;

        if(language < 0 || (int)language >= LocalizationSettings.AvailableLocales.Locales.Count)
        {
            Debug.LogError($"更換語言錯誤: {language}");
            return;
        }

        StartCoroutine(IChangeLanguage(language));
    }

    private IEnumerator IChangeLanguage(Language language)
    {
        IsChanging = true;

        if (!LocalizationSettings.InitializationOperation.IsDone)
            yield return LocalizationSettings.InitializationOperation;

        var targetLocale = LocalizationSettings.AvailableLocales.Locales[(int)language];
        LocalizationSettings.SelectedLocale = targetLocale;

        yield return LocalizationSettings.SelectedLocaleAsync;

        IsChanging = false;
    }

    /// <summary>
    /// 變更字串內容
    /// </summary>
    public void UpdateKey(LocalizeStringEvent localizeEvent, string newKey)
    {
        if (localizeEvent != null)
            localizeEvent.StringReference.SetReference(TableName, newKey);
    }
}
