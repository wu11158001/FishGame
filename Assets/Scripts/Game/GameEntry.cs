using System;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;
using System.Collections;

public class GameEntry : MonoBehaviour
{
    // 是否正在配對(防止重複觸發)
    bool IsMatchmaking;

    Dictionary<CheckJoinRoomDataEnum, bool> CheckJoinRoomDic = new();
    Coroutine MatchmakingCoroutine;

    private void OnDestroy()
    {
        if (NetworkRunnerManagement.Instance != null)
            NetworkRunnerManagement.Instance.RoomListUpdatedEvent -= OnRoomListUpdatedUpdate;
    }

    private void Start()
    {
        if (NetworkRunnerManagement.Instance != null)
            NetworkRunnerManagement.Instance.RoomListUpdatedEvent += OnRoomListUpdatedUpdate;
    }

    public void SetData(LevelEnum levelType)
    {
        StartJoInGame(levelType);
    }

    /// <summary>
    /// 開始加入遊戲
    /// </summary>
    private async void StartJoInGame(LevelEnum levelType)
    {
        try
        {
            IsMatchmaking = true;
            CheckJoinRoomDic.Clear();

            foreach (CheckJoinRoomDataEnum item in Enum.GetValues(typeof(CheckJoinRoomDataEnum)))
            {
                CheckJoinRoomDic.Add(item, false);
            }

            // 產生遊戲入口物件
            await AddressableManagement.Instance.CreateGamePrefab(
                prefabType: GamePrefabEnum.GameTempData,
                callback: (obj) =>
                {
                    GameTempData gameTempData = obj.GetComponent<GameTempData>();
                    if (gameTempData != null)
                    {
                        FirestoreDataManagement.Instance.GameTempData = gameTempData;

                        gameTempData.Initialize();

                        // 獲取所有魚資料
                        gameTempData.GetAllFishData(callback: CheckJoinRoomData);

                        // 獲取所有砲台資料
                        gameTempData.GetAllTurretData(callback: CheckJoinRoomData);

                        // 獲取關卡資料
                        gameTempData.GetCurrentLevelData(levelType: levelType, callback: CheckJoinRoomData);

                        // 開始監聽關卡資料
                        gameTempData.StartListenLevelData(levelType: levelType);

                        // 獲取帳戶資料
                        gameTempData.GetTempAccountData(callback: CheckJoinRoomData);
                    }
                });
        }
        catch (Exception e)
        {
            Debug.LogError($"加入遊戲錯誤 : {e}");
            JoinRoomError();
        }        
    }

    /// <summary>
    /// 加入房間錯誤
    /// </summary>
    private void JoinRoomError()
    {
        // 回大廳
        if (SceneManagement.Instance != null)
        {
            SceneManagement.Instance.LoadScene(
                sceneEnum: SceneEnum.Lobby,
                callback: async () =>
                {
                    if (AddressableManagement.Instance != null)
                        await AddressableManagement.Instance.OpenLobbyView();
                });
        }
    }

    /// <summary>
    /// 檢查加入房間資料獲取狀態
    /// </summary>
    private void CheckJoinRoomData(CheckJoinRoomDataEnum dataType, bool isSuccess)
    {
        if (!CheckJoinRoomDic.ContainsKey(dataType) || !isSuccess)
        {
            Debug.LogError($"檢查加入房間資料獲取狀態錯誤: {dataType}");
            JoinRoomError();
            return;
        }

        CheckJoinRoomDic[dataType] = true;

        if (CheckJoinRoomDic.All(x => x.Value == true))
        {
            Debug.Log("進入房間資料獲取完成");
            JoInGameLobby();
        }
    }

    /// <summary>
    /// 加入遊戲大廳
    /// </summary>
    private async void JoInGameLobby()
    {
        try
        {
            // 重置一個新的 Runner
            NetworkRunnerManagement.Instance.ResetRunner();

            var runner = NetworkRunnerManagement.Instance.NetworkRunner;
            runner.ProvideInput = true;

            var result = await runner.JoinSessionLobby(SessionLobby.Shared);

            if (result.Ok)
            {
                Debug.Log("成功加入大廳，啟動 Coroutine 等待列表同步...");

                // 停止舊的 Coroutine (如果有) 並開啟新的
                if (MatchmakingCoroutine != null) StopCoroutine(MatchmakingCoroutine);
                MatchmakingCoroutine = StartCoroutine(WaitAndCheckRoomList());
            }
            else
            {
                Debug.LogError("加入大廳失敗!");
                JoinRoomError();
            }
        }
        catch (Exception)
        {
            JoinRoomError();
        }
    }

    /// <summary>
    /// 等待檢查房間列表
    /// </summary>
    /// <returns></returns>
    private IEnumerator WaitAndCheckRoomList()
    {
        yield return new WaitForSeconds(2f);

        // 等待超時
        if (IsMatchmaking)
        {
            Debug.Log("等待超時，未發現現有房間，準備自行創建...");
            JoinRoom(Guid.NewGuid().ToString());
        }
    }

    /// <summary>
    /// 房間列表更新
    /// </summary>
    private void OnRoomListUpdatedUpdate(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (!IsMatchmaking) return;

        // 尋找第一個還沒滿且開啟中的房間
        SessionInfo availableSession = sessionList.FirstOrDefault(s => s.IsOpen && s.PlayerCount < s.MaxPlayers);

        if (availableSession != null)
        {
            Debug.Log($"[列表更新] 找到可用房間: {availableSession.Name}");
            JoinRoom(availableSession.Name);
        }
    }

    /// <summary>
    /// 加入房間
    /// </summary>
    private async void JoinRoom(string sessionName)
    {
        // 防止 到期後觸發重複創建房間
        if (MatchmakingCoroutine != null)
        {
            StopCoroutine(MatchmakingCoroutine);
            MatchmakingCoroutine = null;
        }

        // 如果已經不在配對狀態，跳出（防止複數次觸發）
        if (!IsMatchmaking) return;
        IsMatchmaking = false;

        Debug.Log($"準備進入房間: {sessionName}");

        // 執行 StartGame
        var result = await NetworkRunnerManagement.Instance.StartGame(sessionName);

        if (!result.Ok)
        {
            Debug.LogError($"無法加入房間: {result.ShutdownReason}");
            JoinRoomError();
        }
        else
        {
            // 加入房間成功移除房間列表監聽
            if (NetworkRunnerManagement.Instance != null)
                NetworkRunnerManagement.Instance.RoomListUpdatedEvent -= OnRoomListUpdatedUpdate;
        }
    }
}
