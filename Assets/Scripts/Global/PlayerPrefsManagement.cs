using UnityEngine;
using System;

public static class PlayerPrefsManagement
{
    /// <summary> 玩家登入資料 </summary>
    public static string LOGIN_INFO = "LoginInfo";

    /// <summary>
    /// 獲取玩家登入資料
    /// </summary>
    /// <returns></returns>
    public static LoginInfo GetLoginInfo()
    {
        return JsonUtility.FromJson<LoginInfo>(PlayerPrefs.GetString(LOGIN_INFO));
    }
}

/// <summary>
/// 登入訊息
/// </summary>
[Serializable]
public class LoginInfo
{
    public string Account;
    public string Password;
}
