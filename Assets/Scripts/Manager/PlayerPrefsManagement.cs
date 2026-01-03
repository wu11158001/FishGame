using UnityEngine;
using System;
using System.Collections.Generic;

public static class PlayerPrefsManagement
{
    /// <summary> 玩家登入資料 </summary>
    public static string LOGIN_INFO = "LoginInfo";

    /// <summary> 玩家曾經登入資料 </summary>
    public static string RECORD_LOGIN_INFO = "RECORD_LOGIN_INFO";

    /// <summary>
    /// 獲取玩家登入資料
    /// </summary>
    /// <returns></returns>
    public static LoginInfo GetLoginInfo()
    {
        return JsonUtility.FromJson<LoginInfo>(PlayerPrefs.GetString(LOGIN_INFO));
    }

    /// <summary>
    /// 玩家曾經登入資料
    /// </summary>
    public static RecordLoginInfo GetRecordLoginInfo()
    {
        return JsonUtility.FromJson<RecordLoginInfo>(PlayerPrefs.GetString(RECORD_LOGIN_INFO));
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

/// <summary>
/// 玩家曾經登入資料
/// </summary>
[Serializable]
public class RecordLoginInfo
{
    public List<LoginInfo> RecordLogins;
}
