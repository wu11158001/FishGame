using UnityEngine;
using System.Runtime.InteropServices;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using Fusion;
using Unity.Collections;
using System.Linq;
using System.Collections;

#if !UNITY_WEBGL || UNITY_EDITOR
using Firebase.Firestore;
using Firebase.Extensions;
#endif

public class FirestoreManagement : SingletonMonoBehaviour<FirestoreManagement>
{
#if !UNITY_WEBGL || UNITY_EDITOR
    FirebaseFirestore db;
#endif

    // 用來儲存所有的回調，Key = GUID
    private Dictionary<string, Action<FirestoreResponse>> PendingCallbacks = new();

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
    public void UpdateDataToFirestore(FirestoreCollectionNameEnum path, string docId, Dictionary<string, object> updates, Action<FirestoreResponse> callback)
    {
        string guid = Guid.NewGuid().ToString();
        PendingCallbacks.Add(guid, callback);

#if UNITY_WEBGL && !UNITY_EDITOR
        string json = JsonConvert.SerializeObject(updates);
        UpdateDataToFirestore(path.ToString(), docId, json, gameObject.name, nameof(FirestoreCallback), guid);
#else
        DBInstance();
        DocumentReference docRef = db.Collection(path.ToString()).Document(docId);

        // 直接傳入 Dictionary，Firestore 只會更新裡面有的 Key
        docRef.UpdateAsync(updates).ContinueWithOnMainThread(task => {
            bool isSuccess = !task.IsFaulted && !task.IsCanceled;
            FirestoreResponse response = new()
            {
                Guid = guid,
                IsSuccess = isSuccess,
                Status = isSuccess ? "Success" : "Update Fail",
                JsonData = ""
            };
            FirestoreCallback(JsonUtility.ToJson(response));
        });
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
    /// 獲取集合內所有資料
    /// </summary>
    [DllImport("__Internal")]
    private static extern void GetAllDocumentsFromCollection(string path, string callbackObj, string callbackMethod, string guid);
    public void GetAllDocumentsFromCollection(FirestoreCollectionNameEnum path, Action<FirestoreResponse> callback)
    {
        string guid = Guid.NewGuid().ToString();
        PendingCallbacks.Add(guid, callback);

#if UNITY_WEBGL && !UNITY_EDITOR
        GetAllDocumentsFromCollection(path.ToString(), gameObject.name, nameof(FirestoreCallback), guid);
#else
        DBInstance();
        db.Collection(path.ToString()).GetSnapshotAsync().ContinueWithOnMainThread(task => {
            bool isSuccess = false;
            string jsonData = "[]";

            if (!task.IsFaulted && !task.IsCanceled)
            {
                isSuccess = true;
                // 將所有 Document 轉成 List 後序列化成 JSON 陣列
                var allDocs = task.Result.Documents.Select(d => d.ToDictionary()).ToList();
                jsonData = JsonConvert.SerializeObject(allDocs);
            }

            FirestoreResponse response = new()
            {
                Guid = guid,
                IsSuccess = isSuccess,
                Status = isSuccess ? "Success" : "Error",
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
    public void FirestoreCallback(string jsonResponse)
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
    public static extern void ListenToFirestoreData(string path, string docId, string callbackObj, string callbackMethod);

    /// <summary>
    /// 停止監聽
    /// </summary>
    [DllImport("__Internal")]
    public static extern void StopListenToFirestoreData(string docId);

    #endregion

    #region Web事件

    /// <summary>
    /// 綁定滑鼠鼠移出Canvas事件
    /// </summary>
    [DllImport("__Internal")]
    public static extern void BindMouseEvents(string callbackObj, string leaveCallbackMethod, string enterCallbackMethod);

    #endregion
}

#region Firestore

/// <summary>
/// Firestore 回傳狀態
/// </summary>
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

#endregion

#region 帳戶

/// <summary>
/// 帳戶資料
/// </summary>
[Serializable]
public class AccountData
{
    /// <summary> 註冊時間(時間戳) </summary>
    public string RegisterTime;

    /// <summary> 最後登入時間(時間戳) </summary>
    public string LastLoginTime;

    /// <summary> 心跳包最後更新時間(時間戳) </summary>
    public long HeartbeatUpdateTime;

    /// <summary> 帳號 </summary>
    public string Account;

    /// <summary> 密碼 </summary>
    public string Password;

    /// <summary> 暱稱 </summary>
    public string Nickname;

    /// <summary> 頭像編號 </summary>
    public int Avatar;

    /// <summary> 頭像框編號 </summary>
    public int AvatarFrame;

    /// <summary> 金幣 </summary>
    public double Coins;

    /// <summary> 預設砲台編號 </summary>
    public int DefaultTurret;

    /// <summary> 擁有砲台編號(","隔開) </summary>
    public string OwnTurret;

    /// <summary> 7日已簽到天(","隔開) </summary>
    public string SevenDays;

    /// <summary> 冰凍道具數量 </summary>
    public int FreezeProps;

    /// <summary>
    ///  獲取擁有砲台編號列表
    /// </summary>
    public List<int> GetOwnTurretList()
    {
        List<int> ownList = new();
        if (string.IsNullOrEmpty(OwnTurret)) return new();

        var parts = OwnTurret.Trim().Split(',');
        foreach (var p in parts)
        {
            if (int.TryParse(p, out int id)) ownList.Add(id);
        }

        return ownList;
    }

    /// <summary> 當前階段(休閒/咬分/吐分) </summary>
    private int _gamePeriodIndex;
    public int GamePeriodIndex
    {
        get => _gamePeriodIndex;
        set
        {
            _gamePeriodIndex = value;
            if (Enum.IsDefined(typeof(GamePeriod), value))
                GamePeriod = (GamePeriod)value;
            else
                GamePeriod = GamePeriod.IdlePeriod;
        }
    }
    public GamePeriod GamePeriod;
}

#endregion

#region 遊戲

/// <summary>
/// 魚群資料
/// </summary>
[Serializable]
public class FishData
{
    /// <summary> 識別名稱 </summary>
    private string _fishName;
    public string FishName
    {
        get => _fishName;
        set
        {
            _fishName = value;
            if (Enum.TryParse(_fishName, out NetworkPrefabEnum type))
                FishType = type;
            else
                FishType = NetworkPrefabEnum.None;
        }
    }

    /// <summary> 魚類型 </summary>
    public NetworkPrefabEnum FishType;

    /// <summary> 移動時間 </summary>
    public float Duration;

    /// <summary> 擊中機率(0~1) </summary>
    public double Probability;

    /// <summary> 捕獲當下獎勵金幣倍數 </summary>
    public double Magnification;

    /// <summary> 最小獎勵金幣倍數 </summary>
    public double MinMagnification;

    /// <summary> 最大獎勵金幣倍數 </summary>
    public double MaxMagnification;

    /// <summary>
    /// 轉換成NetworkStruct
    /// </summary>
    public FishData_Network ToNetworkStruct()
    {
        return new FishData_Network
        {
            FishType = this.FishType,
            Duration = this.Duration,
            Probability = this.Probability,
            Magnification = this.Magnification,
            MinMagnification = this.MinMagnification,
            MaxMagnification = this.MaxMagnification,
        };
    }

    /// <summary>
    /// 複製
    /// </summary>
    /// <returns></returns>
    public FishData Clone()
    {
        return (FishData)this.MemberwiseClone();
    }
}

/// <summary>
/// 魚群資料_Network
/// </summary>
public struct FishData_Network : INetworkStruct
{
    /// <summary> 魚類型 </summary>
    public NetworkPrefabEnum FishType;

    /// <summary> 移動時間 </summary>
    public float Duration;

    /// <summary> 擊中機率(0~1) </summary>
    public double Probability;

    /// <summary> 獎勵金幣倍數 </summary>
    public double Magnification;

    /// <summary> 最小獎勵金幣倍數 </summary>
    public double MinMagnification;

    /// <summary> 最大獎勵金幣倍數 </summary>
    public double MaxMagnification;
}

/// <summary>
/// 遊戲關卡資料
/// </summary>
[Serializable]
public class LevelData
{
    /// <summary> 識別名稱 </summary>
    private string _levelName;
    public string LevelName
    {
        get => _levelName;
        set
        {
            _levelName = value;
            if (Enum.TryParse(_levelName, out LevelEnum type))
                LevelType = type;
            else
                LevelType = LevelEnum.ClassicLevel;
        }
    }
    public LevelEnum LevelType;

    /// <summary> 當前階段(休閒/咬分/吐分) </summary>
    private int _gamePeriodIndex;
    public int GamePeriodIndex
    {
        get => _gamePeriodIndex;
        set
        {
            _gamePeriodIndex = value;
            if (Enum.IsDefined(typeof(GamePeriod), value))
                GamePeriod = (GamePeriod)value;
            else
                GamePeriod = GamePeriod.IdlePeriod;
        }
    }
    public GamePeriod GamePeriod;

    /// <summary> 咬分期減少倍率 </summary>
    public double SuckingPeriodLose;

    /// <summary> 吐分期增加倍率 </summary>
    public double PayoutPeriodAdd;

    /// <summary> 咬分期值(休閒期判斷) </summary>
    public double SuckingPeriodValue;

    /// <summary> 吐分期值(休閒期判斷) </summary>
    public double PayoutPeriodValue;

    /// <summary> 累積獎池 </summary>
    public double Jackpot;

    /// <summary> 子彈花費梯度 </summary>
    public double Gradient;

    /// <summary> 最大每發子彈花費 </summary>
    public double MaxCost;

    /// <summary> 最小每發子彈花費 </summary>
    public double MinCost;

    /// <summary> 預設子彈花費 </summary>
    public double DefaultCost;

    /// <summary> 浪潮倒計時 </summary>
    public float WaterWaveTime;

    /// <summary> 特殊魚倒計時 </summary>
    public float SpecialFishTime;
}

#endregion

#region 商店

/// <summary>
/// 砲台資料
/// </summary>
[Serializable]
public class TurretData
{
    /// <summary> 識別名稱 </summary>
    private string _turretName;
    public string TurretName
    {
        get => _turretName;
        set
        {
            _turretName = value;
            if (Enum.TryParse(_turretName, out TurretEnum type))
                TurretType = type;
            else
                TurretType = TurretEnum.None;
        }
    }
    public TurretEnum TurretType;

    /// <summary> 射擊頻率 </summary>
    public float Rate;

    /// <summary> 價格 </summary>
    public double Price;

    /// <summary> 砲孔數量 </summary>
    public int HoleCount;
}

/// <summary>
/// 金幣商店資料
/// </summary>
[Serializable]
public class CoinStoreData
{
    /// <summary> 識別名稱 </summary>
    private string _coinName;
    public string CoinName
    {
        get => _coinName;
        set
        {
            _coinName = value;
            if (Enum.TryParse(_coinName, out ShopCoinEnum type))
                CoinType = type;
            else
                CoinType = ShopCoinEnum.None;
        }
    }

    public ShopCoinEnum CoinType;

    /// <summary> 價格 </summary>
    public double Price;

    /// <summary> 獲得金幣 </summary>
    public double GetCoin;
}

/// <summary>
/// 道具商店資料
/// </summary>
[Serializable]
public class PropsStoreData
{
    /// <summary> 識別名稱 </summary>
    private string _propsName;
    public string PropsName
    {
        get => _propsName;
        set
        {
            _propsName = value;
            if (Enum.TryParse(_propsName, out PropsEnum type))
                PropsType = type;
            else
                PropsType = PropsEnum.None;
        }
    }

    public PropsEnum PropsType;

    /// <summary> 單價價格 </summary>
    public double UnitPrice;
}

#endregion

#region 活動

/// <summary>
/// 登入與註冊獎勵資料
/// </summary>
[Serializable]
public class LoginAndRegisterData
{
    /// <summary> 每日登入獎勵 </summary>
    public double LoginReward;

    /// <summary> 註冊獎勵 </summary>
    public double RegisterReward;
}

/// <summary>
/// 7日獎勵資料
/// </summary>
[Serializable]
public class SevenDayData
{
    public double Day_0;
    public double Day_1;
    public double Day_2;
    public double Day_3;
    public double Day_4;
    public double Day_5;
    public double Day_6;
}

#endregion