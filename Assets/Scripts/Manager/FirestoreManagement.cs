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
#if UNITY_WEBGL && !UNITY_EDITOR
        SaveDataToFirestore(collectionName.ToString(), docId, jsonData, callbackObjName, callbackMethod);
#else
        Debug.Log($"編輯器寫入新資料:\n {jsonData}");
#endif
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
#if UNITY_WEBGL && !UNITY_EDITOR
        UpdateDataToFirestore(collectionName.ToString(), docId, jsonData, callbackObjName, callbackMethod);
#else
        Debug.Log($"編輯器更新資料:\n {jsonData}");
#endif
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
#if UNITY_WEBGL && !UNITY_EDITOR
            GetDataFromFirestore(collectionName.ToString(), docId, callbackObjName, callbackMethod);
#else
        Debug.Log($"編輯器查詢與讀取資料");
#endif
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
