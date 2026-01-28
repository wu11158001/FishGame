using Firebase.Firestore;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirestoreDataManagement : SingletonMonoBehaviour<FirestoreDataManagement>
{
#if !UNITY_WEBGL || UNITY_EDITOR
    // 專門存儲 Editor 環境下的監聽器(Key: docId, Value: 監聽器執行個體)
    private Dictionary<string, ListenerRegistration> EditorListeners = new();
#endif

    // 心跳包Coroutine
    Coroutine HeartbeatCoroutine;
    // 心跳包發送間格時間(秒)
    public int HeartbeatTime { get; private set; } = 20;

    // 當前登入帳戶資料
    public AccountData CurrAccountData { get; private set; } = new();
    // 當下登入帳戶訊息
    public LoginInfo CurrLoginInfo { get; set; }

    // 帳戶資料變更監聽
    public delegate void AccountDataChange(AccountData accountData);
    public event AccountDataChange AsccountDataChangeDelegate;
    // 帳戶金幣變更監聽
    public delegate void AccountCoinChange(AccountData accountData);
    public event AccountCoinChange AccountCoinDataChangeDelegate;
    // 帳戶砲台資料變更監聽
    public delegate void AccountTurretDataChange(AccountData accountData);
    public event AccountTurretDataChange AccountTurretDataChangeDelegate;

    // 所有關卡資料
    Dictionary<LevelEnum, LevelData> LevelDataDic { get; } = new();
    Action<CheckFixedDataEnum, bool> GetAllLevelDataAction;

    // 登入與註冊獎勵資料
    public LoginAndRegisterData LoginAndRegisterData { get; private set; } = new();
    Action<CheckFixedDataEnum, bool> GetLoginAndRegisterDataAction;

    // 所有砲台資料
    Dictionary<TurretEnum, TurretData> TurretDataDic { get; } = new();
    Action<CheckFixedDataEnum, bool> GetAllTurretDataAction;

    // 金幣商店資料
    Dictionary<ShopCoinEnum, CoinStoreData> CoinStoreDataDic = new();
    Action<CheckFixedDataEnum, bool> GetCoinStoreDataAction;

    // 所有道具商店資料
    Dictionary<PropsEnum, PropsStoreData> PropsStoreDataDic = new();
    Action<CheckFixedDataEnum, bool> GetAllPropsStoreDataAction;

    // 遊戲暫存資料
    public GameTempData GameTempData { get; set; }

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

        StopListenAccountData();
        StopAllCoroutines();
    }

    private void Start()
    {
#if !UNITY_EDITOR && UNITY_WEBGL
        // 綁定滑鼠鼠移出Canvas事件
        FirestoreManagement.BindMouseEvents(gameObject.name, nameof(OnMouseLeaveCanvas), nameof(OnMouseEnterCanvas));
#endif
    }

    /// <summary>
    /// 滑鼠鼠移出Canvas
    /// </summary>
    public void OnMouseLeaveCanvas()
    {
        if(GameTempData != null)
        {
            GameTempData.SendUpdateAccountCoinData();
            GameTempData.SendUpdateLevelDataJackpot();
        }
    }

    /// <summary>
    /// 滑鼠鼠進入Canvas
    /// </summary>
    public void OnMouseEnterCanvas()
    {

    }

    #region 心跳包

    /// <summary>
    /// 停止心跳包發送
    /// </summary>
    public void StopHeartbeat()
    {
        if (HeartbeatCoroutine != null)
            StopCoroutine(HeartbeatCoroutine);

        if(FirestoreManagement.Instance != null)
        {
            var updates = new Dictionary<string, object>
            {
                { "HeartbeatUpdateTime", 0 }
            };

            FirestoreManagement.Instance.UpdateDataToFirestore(
                path: FirestoreCollectionNameEnum.AccountData,
                docId: CurrLoginInfo.Account,
                updates: updates,
                callback: (res) => {
                    if (!res.IsSuccess) Debug.LogError("心跳更新失敗");
                });
        }
    }

    /// <summary>
    /// 開始心跳包發送
    /// </summary>
    public void StartHeartbeat()
    {
        if (HeartbeatCoroutine != null)
            StopCoroutine(HeartbeatCoroutine);

        HeartbeatCoroutine = StartCoroutine(ISendHeartbeat());
    }

    /// <summary>
    /// 心跳包發送
    /// </summary>
    /// <returns></returns>
    private IEnumerator ISendHeartbeat()
    {
        while (true)
        {            
            if(FirestoreManagement.Instance != null)
            {
                // 獲取當前 Unix 時間戳 (秒)
                long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                var updates = new Dictionary<string, object>
                {
                    { "HeartbeatUpdateTime", currentTimestamp }
                };

                FirestoreManagement.Instance.UpdateDataToFirestore(
                    path: FirestoreCollectionNameEnum.AccountData,
                    docId: CurrLoginInfo.Account,
                    updates: updates,
                    callback: (res) => {
                        if (!res.IsSuccess) Debug.LogError("心跳更新失敗");
                    });
            }

            yield return new WaitForSeconds(HeartbeatTime);
        }
    }

    #endregion

    #region 帳戶資料監聽

    /// <summary>
    /// 停止監聽帳戶資料
    /// </summary>
    public void StopListenAccountData()
    {
        if (CurrLoginInfo == null || string.IsNullOrEmpty(CurrLoginInfo.Account))
            return;

        string docId = CurrLoginInfo.Account;

#if UNITY_WEBGL && !UNITY_EDITOR
        FirestoreManagement.StopListenToFirestoreData(docId);
#else
        if (EditorListeners.ContainsKey(docId))
        {
            EditorListeners[docId].Stop();
            EditorListeners.Remove(docId);
        }
#endif
    }

    /// <summary>
    /// 開始監聽帳戶資料
    /// </summary>
    public void StartListenAccountData()
    {
        string path = FirestoreCollectionNameEnum.AccountData.ToString();
        string docId = CurrLoginInfo.Account;

#if UNITY_WEBGL && !UNITY_EDITOR
        FirestoreManagement.ListenToFirestoreData(path, docId, gameObject.name, nameof(OnAccountDataChanged));
#else
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

        EditorListeners.Add(docId, registration);
#endif
    }

    /// <summary>
    /// 帳戶資料變更
    /// </summary>
    public void OnAccountDataChanged(string jsonResponse)
    {
        var response = JsonUtility.FromJson<FirestoreResponse>(jsonResponse);
        AccountData accountData = JsonConvert.DeserializeObject<AccountData>(response.JsonData);

        // 帳戶金幣資料變更
        if (CurrAccountData.Coins != accountData.Coins)
            AccountCoinDataChangeDelegate?.Invoke(accountData);

        // 帳戶砲台資料變更
        if (CurrAccountData.DefaultTurret != accountData.DefaultTurret || CurrAccountData.OwnTurret != accountData.OwnTurret)
            AccountTurretDataChangeDelegate?.Invoke(accountData);

        // 帳戶資料變更
        AsccountDataChangeDelegate?.Invoke(accountData);

        CurrAccountData = accountData;
    }

    #endregion

    #region 登入註冊獎勵資料

    /// <summary>
    /// 獲取登入與獎勵資料
    /// </summary>
    public void GetLoginAndRegisterData(Action<CheckFixedDataEnum, bool> callback)
    {
        GetLoginAndRegisterDataAction = callback;

        FirestoreManagement.Instance.GetDataFromFirestore(
            path: FirestoreCollectionNameEnum.ActivityData,
            docId: FirestoreActivityDataFileNameEnum.LoginAndRegister.ToString(),
            callback: GetLoginRewardCallback);
    }

    /// <summary>
    /// 獲取登入獎勵資料Callback
    /// </summary>
    private void GetLoginRewardCallback(FirestoreResponse response)
    {
        if (response.IsSuccess)
        {
            try
            {
                LoginAndRegisterData = JsonConvert.DeserializeObject<LoginAndRegisterData>(response.JsonData);
                Debug.Log("獲取登入獎勵資料完成。");
                GetLoginAndRegisterDataAction?.Invoke(CheckFixedDataEnum.LoginAndRegisterData, true);
            }
            catch (Exception e)
            {
                Debug.LogError($"獲取獲取登入獎勵資料錯誤: {e}");
                GetLoginAndRegisterDataAction?.Invoke(CheckFixedDataEnum.LoginAndRegisterData, false);
            }
        }
    }

    #endregion

    #region 所有砲台資料

    /// <summary>
    /// 獲取所有砲台資料
    /// </summary>
    public void GetAllTurretData(Action<CheckFixedDataEnum, bool> callback)
    {
        GetAllTurretDataAction = callback;

        FirestoreManagement.Instance.GetAllDocumentsFromCollection(
                path: FirestoreCollectionNameEnum.TurretData,
                callback: GetAllTurretDataCallback);
    }

    /// <summary>
    /// 獲取所有砲台資料Callback
    /// </summary>
    public void GetAllTurretDataCallback(FirestoreResponse response)
    {
        if (response.IsSuccess)
        {
            TurretDataDic.Clear();
            List<TurretData> turretList = JsonConvert.DeserializeObject<List<TurretData>>(response.JsonData);

            foreach (var data in turretList)
            {
                TurretDataDic.Add(data.TurretType, data);
            }

            Debug.Log("獲取所有砲台資料完成。");
            GetAllTurretDataAction?.Invoke(CheckFixedDataEnum.AllTurretData, true);
        }
        else
        {
            Debug.LogError($"獲取所有砲台資料失敗");
            AddressableManagement.Instance.ShowToast("Wiring Error");
            GetAllTurretDataAction?.Invoke(CheckFixedDataEnum.AllTurretData, false);
        }
    }

    /// <summary>
    /// 獲取砲台資料
    /// </summary>
    public TurretData GetTurrethData(TurretEnum turretType)
    {
        // 嘗試從字典中獲取資料
        if (TurretDataDic.TryGetValue(turretType, out TurretData data))
        {
            return data;
        }

        Debug.LogWarning($"找不到砲台資料: {turretType}");
        return null;
    }

    #endregion

    #region 所有關卡資料

    /// <summary>
    /// 獲取所有關卡資料
    /// </summary>
    public void GetAllLevelData(Action<CheckFixedDataEnum, bool> callback)
    {
        GetAllLevelDataAction = callback;

        FirestoreManagement.Instance.GetAllDocumentsFromCollection(
                path: FirestoreCollectionNameEnum.LevelData,
                callback: GetAllLevelDataCallback);
    }

    /// <summary>
    /// 獲取所有關卡資料Callback
    /// </summary>
    private void GetAllLevelDataCallback(FirestoreResponse response)
    {
        if (response.IsSuccess)
        {
            LevelDataDic.Clear();
            List<LevelData> levelList = JsonConvert.DeserializeObject<List<LevelData>>(response.JsonData);

            foreach (var data in levelList)
            {
                LevelDataDic.Add(data.LevelType, data);
            }

            Debug.Log("獲取所有關卡資料完成。");
            GetAllLevelDataAction?.Invoke(CheckFixedDataEnum.AllLevelData, true);
        }
        else
        {
            Debug.LogError($"獲取所有關卡資料失敗");
            AddressableManagement.Instance.ShowToast("Wiring Error");
            GetAllLevelDataAction?.Invoke(CheckFixedDataEnum.AllLevelData, false);
        }
    }

    /// <summary>
    /// 獲取關卡資料
    /// </summary>
    public LevelData GetLevelData(LevelEnum levelType)
    {
        // 嘗試從字典中獲取資料
        if (LevelDataDic.TryGetValue(levelType, out LevelData data))
        {
            return data;
        }

        Debug.LogWarning($"找不到關卡資料: {levelType}");
        return null;
    }

    #endregion

    #region 金幣商店資料

    /// <summary>
    /// 獲取金幣商店資料
    /// </summary>
    public void GetCoinStoreData(Action<CheckFixedDataEnum, bool> callback)
    {
        GetCoinStoreDataAction = callback;

        if (FirestoreManagement.Instance != null)
        {
            FirestoreManagement.Instance.GetAllDocumentsFromCollection(
                path: FirestoreCollectionNameEnum.CoinStoreData,
                callback: GetCoinStoreDataCallback);
        }
    }

    /// <summary>
    /// 獲取金幣商店資料Callback
    /// </summary>
    private void GetCoinStoreDataCallback(FirestoreResponse response)
    {
        if (response.IsSuccess)
        {
            try
            {
                List<CoinStoreData> coinList = JsonConvert.DeserializeObject<List<CoinStoreData>>(response.JsonData);

                foreach (var data in coinList)
                {
                    CoinStoreDataDic.Add(data.CoinType, data);
                }

                Debug.Log("獲取金幣商店資料完成。");
                GetCoinStoreDataAction?.Invoke(CheckFixedDataEnum.CoinStoreData, true);
            }
            catch (Exception e)
            {
                Debug.LogError($"獲取金幣商店資料錯誤: {e}");
                GetCoinStoreDataAction?.Invoke(CheckFixedDataEnum.CoinStoreData, false);
            }
        }
    }

    /// <summary>
    /// 獲取商店金幣資料
    /// </summary>
    public CoinStoreData GetCoinStoreData(ShopCoinEnum coinType)
    {
        // 嘗試從字典中獲取資料
        if (CoinStoreDataDic.TryGetValue(coinType, out CoinStoreData data))
        {
            return data;
        }

        Debug.LogWarning($"找不到商店金幣資料: {coinType}");
        return null;
    }

    #endregion

    #region 道具商店資料

    /// <summary>
    /// 獲取所有道具商店資料
    /// </summary>
    public void GetAllPropsStoreData(Action<CheckFixedDataEnum, bool> callback)
    {
        GetAllPropsStoreDataAction = callback;

        if (FirestoreManagement.Instance != null)
        {
            FirestoreManagement.Instance.GetAllDocumentsFromCollection(
                path: FirestoreCollectionNameEnum.PropsStoreData,
                callback: GetAllPropsStoreDataCallback);
        }
    }

    /// <summary>
    /// 獲取所有道具商店資料Callback
    /// </summary>
    private void GetAllPropsStoreDataCallback(FirestoreResponse response)
    {
        if (response.IsSuccess)
        {
            try
            {
                List<PropsStoreData> propsList = JsonConvert.DeserializeObject<List<PropsStoreData>>(response.JsonData);

                foreach (var data in propsList)
                {
                    PropsStoreDataDic.Add(data.PropsType, data);
                }

                Debug.Log("獲取所有道具商店資料完成。");
                GetAllPropsStoreDataAction?.Invoke(CheckFixedDataEnum.PropsStoreData, true);
            }
            catch (Exception e)
            {
                Debug.LogError($"獲取所有道具商店資料錯誤: {e}");
                GetAllPropsStoreDataAction?.Invoke(CheckFixedDataEnum.PropsStoreData, false);
            }
        }
    }

    /// <summary>
    /// 獲取商店道具資料
    /// </summary>
    public PropsStoreData GetPropsStoreData(PropsEnum propsType)
    {
        // 嘗試從字典中獲取資料
        if (PropsStoreDataDic.TryGetValue(propsType, out PropsStoreData data))
        {
            return data;
        }

        Debug.LogWarning($"找不到商店道具資料: {propsType}");
        return null;
    }

    #endregion
}
