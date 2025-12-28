using UnityEngine;
using System.Runtime.InteropServices;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;

#if !UNITY_WEBGL || UNITY_EDITOR
using Firebase.Firestore;
using Firebase.Extensions;
#endif

public class FirestoreManagement : SingletonMonoBehaviour<FirestoreManagement>
{
#if !UNITY_WEBGL || UNITY_EDITOR
    FirebaseFirestore db;

    // 專門存儲 Editor 環境下的監聽器(Key: docId, Value: 監聽器執行個體)
    private Dictionary<string, ListenerRegistration> EditorListeners = new();
#endif

    // 用來儲存所有的回調，Key = GUID
    private Dictionary<string, Action<FirestoreResponse>> PendingCallbacks = new();

    public delegate void AccountDataChange(FirestoreResponse response);
    public event AccountDataChange AsccountDataChangeDelete;

    protected override void OnDestroy()
    {
        base.OnDestroy();

#if !UNITY_WEBGL || UNITY_EDITOR
        foreach (var listener in EditorListeners.Values)
        {
            listener.Stop();
        }
        EditorListeners.Clear();
#endif
    }

    public void OnDataChanged(string jsonResponse)
    {
        // 處理資料邏輯...
        Debug.Log("收到回傳: " + jsonResponse);
    }

    /// <summary>
    /// DB初始化
    /// </summary>
    private void DBInstance()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (db == null)
        {
            db = FirebaseFirestore.DefaultInstance;
        }
#endif
    }

    /// <summary>
    /// 獲取Json資料轉字典
    /// </summary>
    private Dictionary<string, object> GetJsonDataToDictionary(string jsonData)
    {      
        if(!string.IsNullOrEmpty(jsonData))
        {
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
        }

        return null;
    }

    #region Firestore資料處理

    /// <summary>
    /// 寫入新資料
    /// </summary>
    [DllImport("__Internal")]
    private static extern void SaveDataToFirestore(string path, string docId, string jsonData, string callbackObj, string callbackMethod, string guid);
    public void SaveDataToFirestore(FirestoreCollectionNameEnum path, string docId, string jsonData, Action<FirestoreResponse> callback)
    {
        string guid = Guid.NewGuid().ToString();
        PendingCallbacks.Add(guid, callback);

#if UNITY_WEBGL && !UNITY_EDITOR
        SaveDataToFirestore(path.ToString(), docId, jsonData, gameObject.name, nameof(FirestoreCallback), guid);
#else
        DBInstance();
        var dict = GetJsonDataToDictionary(jsonData);

        if(dict != null)
        {
            DocumentReference docRef = db.Collection(path.ToString()).Document(docId);
            docRef.SetAsync(dict).ContinueWithOnMainThread(task => {
                bool isSuccess = !task.IsFaulted && !task.IsCanceled;
                string status = isSuccess ? "Success" : "WriteFail";

                FirestoreResponse response = new()
                {
                    Guid = guid,
                    IsSuccess = isSuccess,
                    Status = status,
                    JsonData = jsonData
                };
                FirestoreCallback(JsonUtility.ToJson(response)); ;
            });
        }
#endif
    }

    /// <summary>
    /// 更新資料
    /// </summary>
    [DllImport("__Internal")]
    private static extern void UpdateDataToFirestore(string path, string docId, string jsonData, string callbackObj, string callbackMethod, string guid);
    public void UpdateDataToFirestore(FirestoreCollectionNameEnum path, string docId, string jsonData, Action<FirestoreResponse> callback)
    {
        string guid = Guid.NewGuid().ToString();
        PendingCallbacks.Add(guid, callback);

#if UNITY_WEBGL && !UNITY_EDITOR
        UpdateDataToFirestore(path.ToString(), docId, jsonData, gameObject.name, nameof(FirestoreCallback), guid);
#else
        DBInstance();
        var dict = GetJsonDataToDictionary(jsonData);

        if(dict != null)
        {
            DocumentReference docRef = db.Collection(path.ToString()).Document(docId);

            docRef.UpdateAsync(dict).ContinueWithOnMainThread(task => {

                bool isSuccess = true;
                string status = "Success";

                if (task.IsFaulted)
                {
                    isSuccess = false;
                    status = "Update Fail";
                    Debug.LogError($"更新失敗: {task.Exception}");
                }
                else if (task.IsCanceled)
                {
                    isSuccess = false;
                    status = "UpdateFail";
                    Debug.LogError($"更新失敗: Task Canceled");
                }

                FirestoreResponse response = new()
                {
                    Guid = guid,
                    IsSuccess = isSuccess,
                    Status = status,
                    JsonData = jsonData
                };

                FirestoreCallback(JsonUtility.ToJson(response));
            });
        }
#endif
    }

    /// <summary>
    /// 查詢與讀取資料
    /// </summary>
    [DllImport("__Internal")]
    private static extern void GetDataFromFirestore(string path, string docId, string callbackObj, string callbackMethod, string guid);
    public void GetDataFromFirestore(FirestoreCollectionNameEnum path, string docId, Action<FirestoreResponse> callback)
    {
        string guid = Guid.NewGuid().ToString();
        PendingCallbacks.Add(guid, callback);

#if UNITY_WEBGL && !UNITY_EDITOR
        GetDataFromFirestore(path.ToString(), docId, gameObject.name, nameof(FirestoreCallback), guid);
#else
        DBInstance();

        db.Collection(path.ToString()).Document(docId).GetSnapshotAsync().ContinueWithOnMainThread(task => {
            bool isSuccess = false;
            string status = "Success";
            string jsonData = "";

            if (task.IsFaulted || task.IsCanceled)
            {
                status = "Error";
            }
            else
            {
                DocumentSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    isSuccess = true;
                    status = "Success";
                    var dataDict = snapshot.ToDictionary();
                    // 確保 dataDict 不為 null
                    jsonData = dataDict != null ? JsonConvert.SerializeObject(dataDict) : "{}";
                }
                else
                {
                    status = "AccountNotFound";
                }
            }

            FirestoreResponse response = new()
            {
                Guid = guid,
                IsSuccess = isSuccess,
                Status = status,
                JsonData = jsonData
            };

            FirestoreCallback(JsonUtility.ToJson(response));
        });
#endif
    }

    /// <summary>
    /// 刪除資料
    /// </summary>
    [DllImport("__Internal")]
    private static extern void DeleteDataFromFirestore(string path, string docId, string callbackObj, string callbackMethod, string guid);
    public void DeleteDataFromFirestore(FirestoreCollectionNameEnum path, string docId, Action<FirestoreResponse> callback)
    {
        string guid = Guid.NewGuid().ToString();
        PendingCallbacks.Add(guid, callback);

#if UNITY_WEBGL && !UNITY_EDITOR
        DeleteDataFromFirestore(path.ToString(), docId, gameObject.name, nameof(FirestoreCallback), guid);
#else
        DBInstance();

        DocumentReference docRef = db.Collection(path.ToString()).Document(docId);
        docRef.DeleteAsync().ContinueWithOnMainThread(task => {

            bool isSuccess = true;
            string status = "Success";

            if (task.IsFaulted)
            {
                isSuccess = false;
                status = "DeleteError";
                Debug.LogError($"刪除資料失敗: {task.Exception}");
            }
            else if (task.IsCanceled)
            {
                isSuccess = false;
                status = "DeleteError";
            }

            FirestoreResponse response = new()
            {
                Guid = guid,
                IsSuccess = isSuccess,
                Status = status,
                JsonData = ""
            };

            FirestoreCallback(JsonUtility.ToJson(response));
        });
#endif
    }

    #endregion

    #region Firestore回傳處理

    /// <summary>
    /// 所有Firetstre回傳
    /// </summary>
    private void FirestoreCallback(string jsonResponse)
    {
        // 解析基礎回應結構
        var response = JsonUtility.FromJson<FirestoreResponse>(jsonResponse);

        if (PendingCallbacks.ContainsKey(response.Guid))
        {
            PendingCallbacks[response.Guid]?.Invoke(response);
            PendingCallbacks.Remove(response.Guid);
        }
    }

    /// <summary>
    /// 顯示回傳資料失敗處理
    /// </summary>
    public void CallbackFailHandle(FirestoreStatusEnum status)
    {
        switch(status)
        {
            // 連線錯誤!
            case FirestoreStatusEnum.Error:
                AddressableManagement.Instance.ShowToast("Wiring Error");
                break;

            // 帳號資料不存在!!
            case FirestoreStatusEnum.AccountNotFound:
                AddressableManagement.Instance.ShowToast("Account Error");
                break;

            // 刪除資料失敗!
            case FirestoreStatusEnum.DeleteError:
                AddressableManagement.Instance.ShowToast("Delete Error");
                break;

            // 更新失敗!
            case FirestoreStatusEnum.UpdateFail:
                AddressableManagement.Instance.ShowToast("Update Fail");
                break;

            // 寫入資料失敗!!
            case FirestoreStatusEnum.WriteFail:
                AddressableManagement.Instance.ShowToast("Writ Fail");
                break;
        }
    }

    #endregion

    #region Firestore監聽資料

    /// <summary>
    /// 監聽資料變更
    /// </summary>
    [DllImport("__Internal")]
    private static extern void ListenToFirestoreData(string path, string docId, string callbackObj, string callbackMethod);

    /// <summary>
    /// 停止監聽
    /// </summary>
    [DllImport("__Internal")]
    private static extern void StopListenToFirestoreData(string docId);

    /// <summary>
    /// 開始監聽帳戶資料
    /// </summary>
    public void StartListenAccountData()
    {
        string path = FirestoreCollectionNameEnum.AccountData.ToString();
        string docId = PlayerPrefsManagement.GetLoginInfo().Account;

#if UNITY_WEBGL && !UNITY_EDITOR
        ListenToFirestoreData(path, docId, gameObject.name, nameof(OnAccountDataChanged));
        Debug.Log($"[WebGL] 開始監聽: {docId}");
#else
        // 已經在監聽停止
        StopListenAccountData();

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference docRef = db.Collection(path).Document(docId);

        ListenerRegistration registration = docRef.Listen(snapshot => {
            if (snapshot == null) return;

            bool exists = snapshot.Exists;
            string innerJson = exists ? JsonConvert.SerializeObject(snapshot.ToDictionary()) : "";

            var response = new
            {
                IsSuccess = exists,
                Status = exists ? "DataChanged" : "AccountNotFound",
                JsonData = innerJson
            };

            OnAccountDataChanged(JsonConvert.SerializeObject(response));
        });

        // 將監聽器存入 C# 字典
        EditorListeners.Add(docId, registration);
        Debug.Log($"[Editor] 開始監聽帳戶資料: {docId}");
#endif
    }

    /// <summary>
    /// 停止監聽帳戶資料
    /// </summary>
    public void StopListenAccountData()
    {
        string docId = PlayerPrefsManagement.GetLoginInfo().Account;

#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL 端：呼叫 .jslib 刪除 JS 字典裡的監聽
        StopListenToFirestoreData(docId);
#else
        if (EditorListeners.ContainsKey(docId))
        {
            EditorListeners[docId].Stop();
            EditorListeners.Remove(docId);
            Debug.Log($"[Editor] 停止監聽帳戶資料: {docId}");
        }
#endif
    }

    /// <summary>
    /// 帳戶資料變更
    /// </summary>
    public void OnAccountDataChanged(string jsonResponse)
    {
        var response = JsonUtility.FromJson<FirestoreResponse>(jsonResponse);
        AsccountDataChangeDelete?.Invoke(response);
    }

    #endregion
}

[Serializable]
public class FirestoreResponse
{
    /// <summary> 識別回傳方法ID </summary>
    public string Guid;

    /// <summary> 是否成功 </summary>
    public bool IsSuccess;

    /// <summary> 狀態碼(Success, NotFound, Error) </summary>
    public string Status;
    public FirestoreStatusEnum ResponseStatus
    {
        get
        {
            if (Enum.TryParse(Status.Replace(" ", ""), true, out FirestoreStatusEnum status))
            {
                return status;
            }
            return FirestoreStatusEnum.Error;
        }
    }

    /// <summary>  JSON 資料 </summary>
    public string JsonData;
}

/// <summary>
/// 帳戶資料
/// </summary>
[Serializable]
public class AccountData
{
    /// <summary> 帳號 </summary>
    public string Account;

    /// <summary> 密碼 </summary>
    public string Password;

    /// <summary> 金幣 </summary>
    public int Coins;
}
