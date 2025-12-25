using UnityEngine;
using System.Runtime.InteropServices;

public class FirestoreManagement : SingletonMonoBehaviour<FirestoreManagement>
{
    /// <summary>
    /// 寫入新資料
    /// </summary>
    /// <param name="collectionName">集合名稱</param>
    /// <param name="docId">資料表名稱</param>
    /// <param name="jsonData">JSON格式的內容</param>
    /// <param name="callbackObjName">callback物件名稱</param>
    /// <param name="callbackMethod">callback方法</param>
    [DllImport("__Internal")]
    private static extern void SaveDataToFirestore(string collectionName, string docId, string jsonData, string callbackObjName, string callbackMethod);
    public void SaveDataToFirestore(FirestoreCollectionName collectionName, string docId, string jsonData, string callbackObjName, string callbackMethod)
    {
        SaveDataToFirestore(collectionName.ToString(), docId, jsonData, callbackObjName, callbackMethod);
    }

    /// <summary>
    /// 更新資料
    /// </summary>
    /// <param name="collectionName">集合名稱</param>
    /// <param name="docId">資料表名稱</param>
    /// <param name="jsonData">JSON格式的內容</param>
    /// <param name="callbackObjName">callback物件名稱</param>
    /// <param name="callbackMethod">callback方法</param>
    [DllImport("__Internal")]
    private static extern void UpdateDataToFirestore(string collectionName, string docId, string jsonData, string callbackObjName, string callbackMethod);
    public void UpdateDataToFirestore(FirestoreCollectionName collectionName, string docId, string jsonData, string callbackObjName, string callbackMethod)
    {
        UpdateDataToFirestore(collectionName.ToString(), docId, jsonData, callbackObjName, callbackMethod);
    }

    /// <summary>
    /// 查詢與讀取資料
    /// </summary>
    /// <param name="collectionName">集合名稱</param>
    /// <param name="docId">資料表名稱</param>
    /// <param name="callbackObjName">callback物件名稱</param>
    /// <param name="callbackMethod">callback方法</param>
    [DllImport("__Internal")]
    private static extern void GetDataFromFirestore(string collectionName, string docId, string callbackObjName, string callbackMethod);
    public void GetDataFromFirestore(FirestoreCollectionName collectionName, string docId, string callbackObjName, string callbackMethod)
    {
        GetDataFromFirestore(collectionName.ToString(), docId, callbackObjName, callbackMethod);
    }
}

/// <summary>
/// 帳戶資料
/// </summary>
[System.Serializable]
public class AccountData
{
    /// <summary> 帳號 </summary>
    public string Account;

    /// <summary> 密碼 </summary>
    public string Password;

    /// <summary> 金幣 </summary>
    public int Coins;
}
