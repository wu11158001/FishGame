using UnityEngine;
using System;
using System.Collections.Generic;

public static class PlayerPrefsManagement
{
    /// <summary> 玩家登入資料 </summary>
    public static string LOGIN_INFO = "LoginInfo";

    /// <summary> 玩家曾經登入資料 </summary>
    public static string RECORD_LOGIN_INFO = "RECORD_LOGIN_INFO";

    /// <summary> 已登入帳號本地資料 </summary>
    public static string LOCAL_ACCOUNT_DATA = "LOCAL_ACCOUNT_DATA";

    /// <summary>
    /// 獲取上次玩家登入資料
    /// </summary>
    /// <returns></returns>
    public static LoginInfo GetPreLoginInfo()
    {
        return JsonUtility.FromJson<LoginInfo>(PlayerPrefs.GetString(LOGIN_INFO));
    }

    /// <summary>
    /// 獲取玩家曾經登入資料
    /// </summary>
    public static RecordLoginInfo GetRecordLoginInfo()
    {
        return JsonUtility.FromJson<RecordLoginInfo>(PlayerPrefs.GetString(RECORD_LOGIN_INFO));
    }

    /// <summary>
    /// 獲取已登入帳號本地資料
    /// </summary>
    public static LoginAccountData GetLoginAccountData()
    {
        return JsonUtility.FromJson<LoginAccountData>(PlayerPrefs.GetString(LOCAL_ACCOUNT_DATA));
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

/// <summary>
/// 已登入帳號本地資料
/// </summary>
[Serializable]
public class LoginAccountData
{
    /// <summary> 本地記錄金幣 </summary>
    public double LocalCoin;
}
