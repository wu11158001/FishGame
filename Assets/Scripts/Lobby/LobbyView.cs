using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Newtonsoft.Json;
using Fusion;
using System.Collections.Generic;
using System.Linq;

public class LobbyView : BasicView
{
    [SerializeField] TextMeshProUGUI CoinText;
    [SerializeField] Button StartBtn;
    [SerializeField] Button LogoutBtn;

    Dictionary<CheckJoinRoomDataEnum, bool> CheckJoinRoomDic = new();

    bool IsMatchmaking;

    private void OnDestroy()
    {
        if (FirestoreManagement.Instance != null)
            FirestoreManagement.Instance.AsccountDataChangeDelegate -= AccountDataChange;

        if(NetworkRunnerManagement.Instance != null)
            NetworkRunnerManagement.Instance.RoomListUpdatedEvent -= OnRoomListUpdatedUpdate;
    }

    private void Start()
    {
        StartBtn.onClick.AddListener(StartJoInGame);
        LogoutBtn.onClick.AddListener(Logout);

        NetworkRunnerManagement.Instance.RoomListUpdatedEvent += OnRoomListUpdatedUpdate;

        FirestoreManagement.Instance.AsccountDataChangeDelegate += AccountDataChange;
        FirestoreManagement.Instance.StartListenAccountData();
    }

    public void SetData(Action closeAction)
    {
        CloseAction = closeAction;
    }

    /// <summary>
    /// 登出
    /// </summary>
    private void Logout()
    {
        FirestoreManagement.Instance.StopHeartbeat();

        SceneManagement.Instance.LoadScene(
            sceneEnum: SceneEnum.Login,
            callback: async () =>
            {
                await AddressableManagement.Instance.OpenLoginView(isLogout: true);
            });

        CloseAction?.Invoke();
    }

    #region 資料變更監聽

    /// <summary>
    /// 帳戶資料變更
    /// </summary>
    /// <param name="response"></param>
    private void AccountDataChange(FirestoreResponse response)
    {
        if(response != null)
        {
            AccountData data = JsonConvert.DeserializeObject<AccountData>(response.JsonData);
            CoinText.text = StringUtility.CurrencyFormat(data.Coins);

            AddressableManagement.Instance.CloseLoading();
        }
    }

    /// <summary>
    /// 房間列表更新
    /// </summary>
    private void OnRoomListUpdatedUpdate(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (!IsMatchmaking)
            return;

        // 尋找第一個還沒滿的房間
        SessionInfo availableSession = sessionList.FirstOrDefault(s => s.IsOpen && s.PlayerCount < s.MaxPlayers);

        if (availableSession != null)
        {
            Debug.Log($"找到可用房間: {availableSession.Name}，準備加入...");
            JoinRoom(availableSession.Name);
        }
        else
        {
            Debug.Log("目前沒有空房，準備創建新房間...");
            JoinRoom(Guid.NewGuid().ToString());
        }
    }

    #endregion

    #region 加入遊戲

    /// <summary>
    /// 開始加入遊戲
    /// </summary>
    private void StartJoInGame()
    {
        if (IsMatchmaking) return;

        IsMatchmaking = true;
        AddressableManagement.Instance.ShowLoading();
        CheckJoinRoomDic.Clear();

        foreach (CheckJoinRoomDataEnum item in Enum.GetValues(typeof(CheckJoinRoomDataEnum)))
        {
            CheckJoinRoomDic.Add(item, false);
        }

        // 獲取魚群資料
        TempDataManagement.Instance.GetAllFishData(CheckJoinRoomData);

        // 獲取關卡資料
        TempDataManagement.Instance.GetCurrentLevelData(levelType: LevelEnum.ClassicLevel, callback: CheckJoinRoomData);

        // 獲取帳戶資料
        TempDataManagement.Instance.GetTempAccountData(callback: CheckJoinRoomData);
    }

    /// <summary>
    /// 檢查加入房間資料獲取狀態
    /// </summary>
    /// <param name="dataType"></param>
    private void CheckJoinRoomData(CheckJoinRoomDataEnum dataType)
    {
        if(!CheckJoinRoomDic.ContainsKey(dataType))
        {
            Debug.LogError($"檢查加入房間資料獲取狀態錯誤: {dataType}");
            return;
        }

        CheckJoinRoomDic[dataType] = true;

        if(CheckJoinRoomDic.All(x => x.Value == true))
        {
            Debug.Log("進入房間資料獲取完成");
            JoInLobby();
        }
    }

    /// <summary>
    /// 加入大廳
    /// </summary>
    private async void JoInLobby()
    {
        NetworkRunnerManagement.Instance.ResetRunner();

        var runner = NetworkRunnerManagement.Instance.NetworkRunner;

        runner.ProvideInput = true;

        // 加入大廳
        var result = await runner.JoinSessionLobby(SessionLobby.Shared);

        if (!result.Ok)
        {
            Debug.LogError($"無法加入大廳: {result.ShutdownReason}");
            IsMatchmaking = false;
        }
    }

    /// <summary>
    /// 加入房間
    /// </summary>
    private async void JoinRoom(string sessionName)
    {
        var result = await NetworkRunnerManagement.Instance.StartGame(sessionName);

        if (!result.Ok)
        {
            Debug.LogError($"無法加入房間: {result.ShutdownReason}");
            AddressableManagement.Instance.CloseLoading();
            IsMatchmaking = false;
        }
    }

    #endregion
}
