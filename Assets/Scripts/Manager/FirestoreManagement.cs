using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// 帳戶資料
/// </summary>
[System.Serializable]
public class AccountData
{
    public bool LoginState;         // 登入狀態
    public string Account;          // 帳號
    public string Password;         // 密碼
    public int Coins;               // 金幣
}

public class FirestoreManagement : SingletonMonoBehaviour<FirestoreManagement>
{
    /// <summary>
    /// 寫入與更新資料
    /// </summary>
    /// <param name="collectionName">集合名稱</param>
    /// <param name="docId">資料表名稱</param>
    /// <param name="jsonData">JSON格式的內容</param>
    /// <param name="callbackObjName">callback物件名稱</param>
    /// <param name="callbackMethod">callback方法</param>
    [DllImport("__Internal")]
    private static extern void SaveDataToFirestore(string collectionName, string docId, string jsonData, string callbackObjName, string callbackMethod);
    public void SaveDataToFirestore(FirestoreCollectionName name, string docId, string jsonData, string callbackObjName, string callbackMethod)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SaveDataToFirestore(name.ToString(), docId, jsonData);
#else
        Debug.Log($"模擬寫入: {jsonData}");
#endif
    }
}
