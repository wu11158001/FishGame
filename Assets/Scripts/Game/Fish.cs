using UnityEngine;
using Fusion;
using System.Linq;
using System.Collections;

public class Fish : NetworkBehaviour
{
    // 爆金物件
    [SerializeField] GamePrefabEnum CoinTextType = GamePrefabEnum.CoinText_0;
    [SerializeField] GameObject FishModel;
    [SerializeField] Animator Animator;

    // 判斷是否已不存在場上
    [Networked] public NetworkBool IsDie { get; set; }
    // 移動計時器
    [Networked] TickTimer MoveTimer { get; set; }
    // 總移動時間
    [Networked] float TotalDuration { get; set; }

    // 魚資料
    [Networked] FishData_Network FishData_Network { get; set; }
    // 路線資料
    [Networked] FishPathData FishPathData { get; set; }
    // 動畫速度
    [Networked, OnChangedRender(nameof(UpdateAnimationSpeed))]
    float AniSpeed { get; set; }

    Vector3[] LocalPathPoints;

    WayPointMain WayPointMain;
    LocalPool LocalPool;
    Transform CoinTextPool;
    GameTerrain GameTerrain;

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    public void SetData(NetworkPrefabEnum fishType, bool isMirror, int depth, int wayPointId, int skipWaypoint, float customDuration = 0)
    {
        // 獲取魚資料
        FishData fishData = FirestoreDataManagement.Instance?.GameTempData?.GetFishData(fishType);
        if (fishData != null)
        {
            FishData fishDataInstance = fishData.Clone();

            if (customDuration > 0)
                fishDataInstance.Duration = customDuration;

            FishData_Network = fishDataInstance.ToNetworkStruct();
        }

        // 設置路線資料
        FishPathData = new()
        {
            WayPointId = wayPointId,
            IsMirror = isMirror,
            Depth = depth,
            SkipWaypoint = skipWaypoint,
            SpeedMultiplier = 1
        };

        if (WayPointMain == null)
            WayPointMain = GameObject.Find($"{GamePrefabEnum.WayPointMain}").GetComponent<WayPointMain>();

        WayPoint wayPoint = WayPointMain.GetWayPointById(wayPointId);
        int totalPoints = wayPoint.Points.Count;

        // 計算剩餘路徑的長度比例
        float pathRatio = (float)(totalPoints - skipWaypoint - 1) / (totalPoints - 1);
        pathRatio = Mathf.Clamp01(pathRatio);

        // 調整時間：如果是中途出生，總時間應該要縮短，否則魚會在起點發呆
        float baseDuration = FishData_Network.Duration;
        if (customDuration > 0) baseDuration = customDuration;

        // 魚跑完剩下這段路實際需要的時間
        float actualRemainingDuration = baseDuration * pathRatio;

        TotalDuration = baseDuration;
        MoveTimer = TickTimer.CreateFromSeconds(Runner, actualRemainingDuration);
        AniSpeed = 1;
    }

    public override void Spawned()
    {
        if (FishModel != null)
            FishModel.SetActive(true);

        SetPathPoints();

        // 初始化位置與面向
        if (LocalPathPoints != null && LocalPathPoints.Length >= 2)
        {
            Vector3 startPos = LocalPathPoints[0];
            startPos.y = FishPathData.Depth;
            transform.position = startPos;

            Vector3 nextPos = LocalPathPoints[1];
            nextPos.y = FishPathData.Depth;
            Vector3 initialDir = nextPos - startPos;
            if (initialDir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(initialDir);
            }
        }

        if(Object.HasStateAuthority)
        {
            AniSpeed = 1;
            IsDie = false;
        }

        if (Animator != null) Animator.speed = AniSpeed;

        // 金龍
        if(FishData_Network.FishType == NetworkPrefabEnum.DragonFish && !Object.HasStateAuthority)
        {
            GetDragonAnimProgress();
        }
    }

    public override void Render()
    {
        if (LocalPathPoints == null || LocalPathPoints.Length < 2)
        {
            SetPathPoints();
        }

        // 同步動畫速度
        if (Animator != null && Animator.speed != AniSpeed)
        {
            Animator.speed = AniSpeed;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || !Object.IsValid)
            return;

        if (IsFreezeTimer())
            return;

        Move();
    }

    #region 控制

    /// <summary>
    /// 是否在冰凍時間
    /// </summary>
    private bool IsFreezeTimer()
    {
        if (GameTerrain == null)
            GameTerrain = FindFirstObjectByType<GameTerrain>();

        if (GameTerrain == null)
            return false;

        bool isFreezing = GameTerrain.FreezeTimer.IsRunning && !GameTerrain.FreezeTimer.Expired(Runner);

        if (Object.HasStateAuthority)
        {
            if (isFreezing)
            {
                // 將移動時間往後推，冰凍結束才不會順移
                MoveTimer = TickTimer.CreateFromSeconds(Runner, (MoveTimer.RemainingTime(Runner) ?? 0) + Runner.DeltaTime);
                // 冰凍期間停止動畫
                if (AniSpeed != 0) AniSpeed = 0;
            }
            else
            {
                if (AniSpeed == 0) AniSpeed = 1;
            }
        }

        return isFreezing;
    }

    /// <summary>
    /// 移動
    /// </summary>
    private void Move()
    {
        if (LocalPathPoints == null || LocalPathPoints.Length < 2) return;

        float elapsed = TotalDuration - (MoveTimer.RemainingTime(Runner) ?? 0);
        float t = Mathf.Clamp01(elapsed / TotalDuration);

        // 1. 計算當前應有的位置
        Vector3 nextPos = GetCatmullRomPosition(t, LocalPathPoints);
        nextPos.y = FishPathData.Depth;

        // 2. 計算方向：預測未來的 t (例如當前 t + 0.01)
        float lookAheadT = Mathf.Clamp01(t + 0.01f);
        Vector3 lookTarget = GetCatmullRomPosition(lookAheadT, LocalPathPoints);

        Vector3 moveDir = lookTarget - nextPos; // 這是曲線的切線方向
        moveDir.y = 0; // 強制水平向，解決 X Z 旋轉問題

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            // 使用 SmoothDamp 或 Slerp 可以讓轉向更平滑，不會瞬間「跳」過去
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Runner.DeltaTime * 10f);
        }

        // 最後更新位置
        transform.position = nextPos;

        if (t >= 1.0f && Object.HasStateAuthority)
        {
            IsDie = true;
            Runner.Despawn(Object);
        }            
    }

    /// <summary>
    /// 取得 Catmull-Rom 座標 (此座標應包含 3D 的 X, Y, Z)
    /// </summary>
    private Vector3 GetCatmullRomPosition(float t, Vector3[] points)
    {
        int count = points.Length;
        // 將 t (0~1) 映射到路徑段數
        float totalSteps = t * (count - 1);
        int i = Mathf.FloorToInt(totalSteps);
        float weight = totalSteps - i;

        if (i >= count - 1) return points[count - 1];

        // 取得四個控制點 (Clamp 確保不越界)
        Vector3 p0 = points[Mathf.Max(i - 1, 0)];
        Vector3 p1 = points[i];
        Vector3 p2 = points[Mathf.Min(i + 1, count - 1)];
        Vector3 p3 = points[Mathf.Min(i + 2, count - 1)];

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * weight +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * weight * weight +
            (-p0 + 3f * p1 - 3f * p2 + p3) * weight * weight * weight
        );
    }

    /// <summary>
    /// 設置剩餘路徑移動時間
    /// </summary>
    public void SetFishDuration(float finishTime)
    {
        RPC_SetFishDuration(finishTime);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetFishDuration(float finishTime)
    {
        if (!Object.HasStateAuthority)
            return;

        float remaining = MoveTimer.RemainingTime(Runner) ?? 0;
        float elapsed = TotalDuration - remaining;
        float currentT = Mathf.Clamp01(elapsed / TotalDuration);

        AniSpeed = 3;

        // 如果已經快跑完了，就不處理
        if (currentT >= 0.99f) return;

        float targetRemainingTime = finishTime;
        float newTotalDuration = targetRemainingTime / (1f - currentT);

        // 更新同步變數
        TotalDuration = newTotalDuration;
        // 重設 Timer
        MoveTimer = TickTimer.CreateFromSeconds(Runner, targetRemainingTime);
    }

    /// <summary>
    /// 更新動畫速度
    /// </summary>
    private void UpdateAnimationSpeed()
    {
        if (Animator != null) Animator.speed = AniSpeed;
    }

    /// <summary>
    /// 設置路線
    /// </summary>
    private void SetPathPoints()
    {
        if (WayPointMain == null)
        {
            var wayPointObj = GameObject.Find($"{GamePrefabEnum.WayPointMain}");
            if (wayPointObj != null)
                WayPointMain = wayPointObj.GetComponent<WayPointMain>();
        }

        if (WayPointMain != null)
        {
            WayPoint wayPoint = WayPointMain.GetWayPointById(FishPathData.WayPointId);

            // 移動路徑獲取
            var query = wayPoint.Points.Select(t => t.position);
            if (FishPathData.IsMirror) query = query.Reverse();
            LocalPathPoints = query.Skip(FishPathData.SkipWaypoint).ToArray();
        }
    }

    #endregion

    #region 擊中判斷與效果

    /// <summary>
    /// 顯示爆金文字
    /// </summary>
    private void ShowCoinText(string str, int seatIndex)
    {
        if (LocalPool == null)
            LocalPool = GameObject.FindFirstObjectByType<LocalPool>();

        if (CoinTextPool == null)
            CoinTextPool = GameObject.Find(LocalPoolNamEnum.CoinTextPool.ToString()).transform;

        if (LocalPool == null || CoinTextPool == null)
            return;

        Vector3 createPos = transform.position;
        createPos.y = 1;

        LocalPool.AcquirePrefabInstance<CoinText>(
            prefabType: CoinTextType,
            parent: CoinTextPool,
            pos: createPos,
            callback: (coinText) =>
            {
                coinText.SetData(str: str, recycleSeatIndex: seatIndex);
            });
    }

    /// <summary>
    /// 擊中效果
    /// </summary>
    private void HitEffect(FishHitData fishHitData)
    {
        // 顯示爆金文字
        ShowCoinText(str: fishHitData.EruptionCoinString.ToString(), seatIndex: fishHitData.SeatIndex);

        // 特殊魚，顯示捕獲介面
        if (FishData_Network.FishType == NetworkPrefabEnum.StingrayFish ||
            FishData_Network.FishType == NetworkPrefabEnum.SharkFish ||
            FishData_Network.FishType == NetworkPrefabEnum.DragonFish)
        {
            string rewardStr = StringUtility.CurrencyFormat(fishHitData.Reward);

            switch (FishData_Network.FishType)
            {
                case NetworkPrefabEnum.SharkFish:
                    rewardStr = "Spin !";
                    break;
            }

            AddressableManagement.Instance.OpenSpecialFishCatchView(
                    seatIndex: fishHitData.SeatIndex,
                    sprite: TextureManagement.Instance.GetFishTexture(FishData_Network.FishType),
                    rewardStr: rewardStr);
        }
    }

    /// <summary>
    /// 顯示輪盤
    /// </summary>
    private void ShowSpinWheel(FishHitData fishHitData)
    {
        if (AddressableManagement.Instance == null || FirestoreDataManagement.Instance == null || FirestoreDataManagement.Instance.GameTempData == null)
            return;

        _ = AddressableManagement.Instance.CreateGamePrefab(
                        prefabType: GamePrefabEnum.SpinWheel,
                        callback: (obj) =>
                        {
                            Vector3 pos = transform.position;
                            pos.y = 2;
                            obj.transform.position = pos;

                            obj.transform.rotation =
                                FirestoreDataManagement.Instance.GameTempData.IsMirror?
                                Quaternion.Euler(90, 180, 0) :
                                Quaternion.Euler(90, 0, 0);

                            SpinWheel spinWheel = obj.GetComponent<SpinWheel>();
                            if (spinWheel != null)
                            {
                                SpinWhellData whellData = new()
                                {
                                    RewardStr = StringUtility.CurrencyFormat(fishHitData.Reward),
                                    MinValu = FishData_Network.MinMagnification,
                                    MaxValu = FishData_Network.MaxMagnification,
                                    TargetIndex = fishHitData.SpinWheelIndex,
                                    SeatIndex = fishHitData.SeatIndex,
                                };

                                spinWheel.SetData(whellData);
                            }
                        });
    }

    /// <summary>
    /// 魚被擊中
    /// </summary>
    public void GetHit(FishHitData fishHitData)
    {
        if(fishHitData.IsLocalShow)
        {
            // 擊中效果
            HitEffect(fishHitData);

            if(fishHitData.SpinWheelIndex >= 0)
            {
                // 輪盤
                ShowSpinWheel(fishHitData);
            }
        }

        if (FishModel != null)
            FishModel.SetActive(false);

        RPC_GetHit(fishHitData);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_GetHit(FishHitData fishHitData)
    {
        // 全域產生效果
        if(!fishHitData.IsLocalShow)
        {
            HitEffect(fishHitData);

            if (fishHitData.SpinWheelIndex >= 0)
            {
                // 輪盤
                ShowSpinWheel(fishHitData);
            }
        }

        if (FishModel != null)
            FishModel.SetActive(false);

        if(Object.HasStateAuthority)
        {
            IsDie = true;
        }

        StartCoroutine(IYieldDespawn());
    }

    /// <summary>
    /// 延遲移除物件，等待RPC傳遞
    /// </summary>
    /// <returns></returns>
    private IEnumerator IYieldDespawn()
    {
        yield return new WaitForSeconds(2);

        if (Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }

    #endregion

    #region 特殊判斷

    /// <summary>
    /// 獲取金龍動畫進度
    /// </summary>
    private void GetDragonAnimProgress()
    {
        Debug.Log("獲取動畫進度");
        RPC_GetDragonAnimProgress();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_GetDragonAnimProgress()
    {
        AnimatorStateInfo stateInfo = Animator.GetCurrentAnimatorStateInfo(0); 
        float progress = stateInfo.normalizedTime;
        string stateName = stateInfo.IsName("LtoR") ? "LtoR" : "RtoL";

        Debug.Log($"動畫進度: {stateName} = {progress}");

        RPC_SendDragonAnimProgress(stateName, progress);
    }

    /// <summary>
    /// 發送給所有人金龍動畫進度
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SendDragonAnimProgress(string stateName, float progress)
    {
        if(!Object.HasStateAuthority)
        {
            Animator.Play(stateName, 0, progress % 1f);
        }
    }

    #endregion

    /// <summary>
    /// 獲取魚資料
    /// </summary>
    public FishData_Network GetFishData()
    {
        return FishData_Network;
    }
}

/// <summary>
/// 路線資料
/// </summary>
public struct FishPathData : INetworkStruct
{
    /// <summary> 路線ID </summary>
    public int WayPointId;

    /// <summary> 是否反向移動 </summary>
    public NetworkBool IsMirror;

    /// <summary> 深度 </summary>
    public int Depth;

    /// <summary> 排除的路線點數量 </summary>
    public int SkipWaypoint;

    /// <summary> 中途加速用的倍率 </summary>
    public float SpeedMultiplier;
}

/// <summary>
/// 魚被捕獲資料
/// </summary>
public struct FishHitData : INetworkStruct
{
    /// <summary> 玩家 </summary>
    public PlayerRef Player;

    /// <summary> 爆金文字 </summary>
    public NetworkString<_32> EruptionCoinString;

    /// <summary> 最終獎勵 </summary>
    public double Reward;

    /// <summary> 移動座位 </summary>
    public int SeatIndex;

    /// <summary> 是否只在本地顯示 </summary>
    public NetworkBool IsLocalShow;

    /// <summary> 輪盤目標Index(-1 = 不顯示) </summary>
    public int SpinWheelIndex;
}